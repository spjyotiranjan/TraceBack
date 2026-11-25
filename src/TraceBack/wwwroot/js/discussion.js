
(() => {
  const section = document.querySelector('[data-discussion]');
  const root = section?.querySelector('[data-discussion-root]');
  if (!root) return;
  const form = section.querySelector('[data-comment-form]');
  const body = form.querySelector('[name="body"]');
  const file = form.querySelector('[name="imageFile"]');
  const alt = form.querySelector('[name="imageAlt"]');
  const parent = form.querySelector('[name="parentId"]');
  const preview = form.querySelector('[data-image-preview]');
  const attach = form.querySelector('[data-attach-image]');
  const clear = form.querySelector('[data-clear-image]');
  const submit = form.querySelector('[data-submit-comment]');
  const cancel = form.querySelector('[data-cancel-reply]');
  const pendingStatus = form.querySelector('[data-comment-pending]');
  const replyLabel = form.querySelector('[data-reply-label]');
  const error = form.querySelector('#comment-error');
  const live = section.querySelector('[data-discussion-announcement]');
  const url = section.dataset.commentsUrl;
  const lists = new Map();
  const items = new Set();
  const sentinels = new Map();
  const observer = new IntersectionObserver(entries => {
    for (const entry of entries) {
      const state = sentinels.get(entry.target);
      if (entry.isIntersecting && state) load(state);
    }
  });
  let previewUrl;
  let posting = false;
  let replyOpener;
  const node = (tag, cls, text) => {
    const element = document.createElement(tag);
    element.className = cls;
    if (text !== undefined) element.textContent = text;
    return element;
  };
  const button = (text, action, kind = 'primary') => {
    const element = node('button', 'btn btn-sm btn-outline-' + kind, text);
    element.type = 'button';
    element.addEventListener('click', action);
    return element;
  };
  function clearImage() {
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    previewUrl = null;
    file.value = '';
    alt.value = '';
    alt.required = false;
    form.querySelector('[data-alt-field]').hidden = true;
    preview.hidden = true;
    preview.querySelector('img').removeAttribute('src');
  }
  function clearReply() {
    parent.value = '';
    replyLabel.hidden = true;
    cancel.hidden = true;
    replyOpener = undefined;
    submit.setAttribute('aria-label', 'Post comment');
  }
  function setComposerState() {
    const blocked = posting || section.dataset.canPost !== 'true';
    for (const control of [attach, clear, submit, cancel]) control.disabled = blocked;
    body.readOnly = alt.readOnly = blocked;
    pendingStatus.hidden = !posting;
    form.setAttribute('aria-busy', String(posting));
  }
  async function message(response) {
    if (response.status === 413) return 'Image exceeds 5 MiB.';
    if (response.status === 401 || response.status === 403 || response.redirected)
      return 'Session expired. Sign in again.';
    try { return (await response.json()).error || 'Request failed. Try again.'; }
    catch { return 'Request failed. Try again.'; }
  }
  function render(comment) {
    const item = node('li', 'discussion-item');
    const article = node('article', 'discussion-comment');
    article.id = 'comment-' + comment.id;
    const meta = node('div', 'discussion-meta');
    const author = node('strong', '', comment.deleted ? 'Deleted comment' : comment.authorName);
    const time = node('time', '', new Date(comment.createdAt).toLocaleString());
    time.dateTime = comment.createdAt;
    const avatar = node('span', 'discussion-avatar', (comment.authorName || '').trim().split(/\s+/u).slice(0, 2)
      .map(word => Array.from(word)[0] || '').join('').toUpperCase());
    avatar.setAttribute('aria-hidden', 'true');
    if (!comment.deleted && avatar.textContent) meta.append(avatar);
    meta.append(author, time);
    article.append(meta);
    if (comment.deleted) article.append(node('p', 'discussion-deleted', 'This comment was deleted.'));
    else {
      if (comment.body) article.append(node('p', 'discussion-body', comment.body));
      if (comment.imageUrl) {
        const imageUrl = url + '/' + encodeURIComponent(comment.id) + '/Image';
        const link = node('a', 'discussion-image-link');
        link.href = imageUrl;
        link.target = '_blank';
        link.rel = 'noopener noreferrer';
        link.setAttribute('aria-label', 'Open image: ' + (comment.imageAlt || 'attachment'));
        const image = node('img', 'discussion-image');
        image.src = imageUrl;
        image.alt = comment.imageAlt || '';
        image.loading = 'lazy';
        link.append(image);
        article.append(link);
      }
    }
    const actions = node('div', 'discussion-item-actions');
    if (section.dataset.canPost === 'true') {
      const reply = button('Reply', () => {
        if (posting || section.dataset.canPost !== 'true') return;
        replyOpener = reply;
        parent.value = comment.id;
        replyLabel.textContent = 'Replying to ' + author.textContent;
        replyLabel.hidden = false;
        cancel.hidden = false;
        submit.setAttribute('aria-label', 'Post reply');
        body.focus();
      });
      reply.dataset.reply = '';
      actions.append(reply);
    }
    if (!comment.deleted && (section.dataset.isAdmin === 'true' || comment.authorId === section.dataset.currentUser))
      actions.append(button('Delete', async () => {
        if (!confirm('Delete this comment? Replies will remain visible.')) return;
        try {
          const response = await fetch(url + '/' + encodeURIComponent(comment.id) + '/Delete', {
            method: 'POST', credentials: 'same-origin',
            headers: { RequestVerificationToken: form.querySelector('[name="__RequestVerificationToken"]').value }
          });
          if (!response.ok) throw new Error(await message(response));
          if (response.redirected) throw new Error('Session expired. Sign in again.');
          article.querySelector('.discussion-body')?.remove();
          article.querySelector('.discussion-image-link')?.remove();
          article.append(node('p', 'discussion-deleted', 'This comment was deleted.'));
          avatar.remove();
          author.textContent = 'Deleted comment';
          actions.querySelector('.btn-outline-danger')?.remove();
          live.textContent = 'Comment deleted. Replies remain visible.';
        } catch (failure) { live.textContent = failure.message; alert(failure.message); }
      }, 'danger'));
    if (actions.childElementCount) article.append(actions);
    item.append(article);
    item.append(createList(comment.id).list);
    return item;
  }
  function createList(parentId) {
    const list = node('ul', parentId ? 'discussion-children' : 'discussion-list');
    if (parentId) list.setAttribute('aria-label', 'Replies');
    const sentinel = node('li', 'discussion-cursor');
    const status = node('span', '');
    sentinel.append(status);
    list.append(sentinel);
    const state = { list, sentinel, status, parentId, cursor: undefined, loading: false, failed: false };
    const retry = button(parentId ? 'Retry replies' : 'Retry comments', () => load(state, true));
    retry.hidden = true;
    retry.addEventListener('blur', () => {
      if (!state.failed && !state.loading) retry.hidden = true;
    });
    sentinel.append(retry);
    state.retry = retry;
    lists.set(parentId, state);
    sentinels.set(sentinel, state);
    if (parentId) observer.observe(sentinel);
    return state;
  }
  function insert(comment, state, fromWrite = false) {
    if (items.has(comment.id)) {
      const existing = document.getElementById('comment-' + comment.id)?.parentElement;
      if (!fromWrite && existing?.parentElement === state.list && existing.hasAttribute('data-new-comment')) {
        delete existing.dataset.newComment;
        state.sentinel.before(existing);
      }
      return;
    }
    items.add(comment.id);
    state.list.querySelector(':scope > .discussion-empty')?.remove();
    const item = render(comment);
    if (fromWrite && state.cursor !== null) {
      item.dataset.newComment = '';
      state.list.append(item);
    } else state.sentinel.before(item);
  }
  async function load(state, retrying = false) {
    const { parentId, cursor, status, sentinel, retry } = state;
    if (state.loading || state.cursor === null || (state.failed && !retrying)) return;
    state.loading = true;
    observer.unobserve(sentinel);
    retry.setAttribute('aria-disabled', 'true');
    const loading = parentId ? 'Loading replies…' : 'Loading discussion…';
    status.textContent = loading;
    if (!parentId || cursor) live.textContent = loading;
    if (!parentId && cursor === undefined) root.setAttribute('aria-busy', 'true');
    let failureMessage = 'Comments could not be loaded. Check your connection and try again.';
    try {
      const pageUrl = new URL(url, location.origin);
      pageUrl.searchParams.set('limit', '20');
      if (parentId) pageUrl.searchParams.set('parentId', parentId);
      if (cursor) pageUrl.searchParams.set('cursor', cursor);
      const response = await fetch(pageUrl, { credentials: 'same-origin' });
      if (response.status === 401 || response.status === 403 || response.redirected) {
        failureMessage = 'Session expired. Sign in again.';
        throw new Error(failureMessage);
      }
      if (!response.ok) throw new Error('Comments request failed.');
      const page = await response.json();
      for (const comment of page.comments) insert(comment, state);
      if (!parentId && cursor === undefined && !state.list.querySelector('.discussion-item'))
        sentinel.before(node('li', 'discussion-empty', section.dataset.canPost === 'true'
          ? 'No comments yet. Start the discussion.' : 'No comments yet.'));
      state.failed = false;
      state.cursor = page.nextCursor && page.nextCursor !== cursor ? page.nextCursor : null;
      status.className = '';
      status.textContent = '';
      // Leave a focused retry in place until the reader tabs away.
      retry.hidden = document.activeElement !== retry;
      if (!retry.hidden) retry.textContent = 'Comments loaded';
      if (page.comments.length) live.textContent = `${page.comments.length} ${parentId ? 'replies' : 'comments'} loaded.`;
      else if (!parentId) live.textContent = 'No more comments.';
      if (state.cursor !== null) observer.observe(sentinel);
      else sentinels.delete(sentinel);
    } catch {
      state.failed = true;
      status.className = 'discussion-error';
      status.textContent = failureMessage + ' ';
      retry.textContent = parentId ? 'Retry replies' : 'Retry comments';
      retry.hidden = false;
      retry.removeAttribute('aria-disabled');
      live.textContent = failureMessage;
    } finally {
      state.loading = false;
      if (!parentId) root.setAttribute('aria-busy', 'false');
    }
  }
  file.addEventListener('change', () => {
    const selected = file.files?.[0];
    if (!selected) return clearImage();
    if (!['image/jpeg', 'image/png', 'image/gif', 'image/webp'].includes(selected.type) ||
        selected.size === 0 || selected.size > 5 * 1024 * 1024) {
      clearImage();
      error.textContent = 'Choose one JPEG, PNG, GIF, or WebP image up to 5 MiB.';
      error.hidden = false;
      return;
    }
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    previewUrl = URL.createObjectURL(selected);
    preview.querySelector('img').src = previewUrl;
    preview.hidden = false;
    form.querySelector('[data-alt-field]').hidden = false;
    alt.required = true;
    error.hidden = true;
  });
  attach.addEventListener('click', () => file.click());
  clear.addEventListener('click', () => { clearImage(); error.hidden = true; attach.focus(); });
  cancel.addEventListener('click', () => {
    const opener = replyOpener;
    clearReply();
    (opener?.isConnected ? opener : body).focus();
  });
  form.addEventListener('submit', async event => {
    event.preventDefault();
    if (posting || section.dataset.canPost !== 'true') return;
    error.hidden = true;
    const text = body.value.trim();
    const description = alt.value.trim();
    const hasImage = file.files.length > 0;
    if ((!text && !hasImage) || text.length > 2000 || (hasImage && (!description || description.length > 200))) {
      error.textContent = hasImage && (!description || description.length > 200)
        ? 'Describe the image in 1 to 200 characters.' : 'Write 1 to 2,000 characters or add an image.';
      error.hidden = false;
      return;
    }
    const payload = new FormData(form);
    const target = lists.get(parent.value || undefined);
    posting = true;
    setComposerState();
    live.textContent = 'Posting comment…';
    try {
      const response = await fetch(form.action, {
        method: 'POST', credentials: 'same-origin', body: payload
      });
      if (response.status === 409) {
        section.dataset.canPost = 'false';
        root.querySelectorAll('[data-reply]').forEach(button => { button.hidden = true; });
        const notice = section.querySelector('[data-comment-readonly]');
        notice.hidden = false;
        live.textContent = notice.textContent;
        return;
      }
      if (!response.ok) throw new Error(await message(response));
      if (response.redirected) throw new Error('Session expired. Sign in again.');
      const comment = await response.json();
      insert(comment, target, true);
      form.reset();
      clearImage();
      clearReply();
      live.textContent = 'Comment posted.';
    } catch (failure) { error.textContent = failure.message; error.hidden = false; }
    finally {
      posting = false;
      setComposerState();
      if (document.activeElement === document.body) body.focus({ preventScroll: true });
    }
  });
  window.addEventListener('pagehide', event => { if (previewUrl && !event.persisted) URL.revokeObjectURL(previewUrl); });
  const thread = createList();
  root.replaceChildren(thread.list);
  form.hidden = section.dataset.canPost !== 'true';
  root.hidden = false;
  load(thread);
})();
