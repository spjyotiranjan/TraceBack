
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
  const error = form.querySelector('#comment-error');
  const live = section.querySelector('[data-discussion-announcement]');
  const url = section.dataset.commentsUrl;
  let previewUrl;
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
  function resetForm() {
    form.reset();
    clearImage();
    error.hidden = true;
    parent.value = '';
    form.querySelector('[data-reply-label]').hidden = true;
    form.querySelector('[data-cancel-reply]').hidden = true;
    form.querySelector('[data-submit-comment]').textContent = 'Post comment';
    section.insertBefore(form, root);
  }
  async function message(response) {
    if (response.status === 413) return 'Image exceeds 5 MB.';
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
    const slot = node('div', 'discussion-reply-slot');
    if (section.dataset.canPost === 'true') actions.append(button('Reply', () => {
      parent.value = comment.id;
      const label = form.querySelector('[data-reply-label]');
      label.textContent = 'Replying to ' + (comment.deleted ? 'a deleted comment' : comment.authorName);
      label.hidden = false;
      form.querySelector('[data-cancel-reply]').hidden = false;
      form.querySelector('[data-submit-comment]').textContent = 'Post reply';
      slot.append(form);
      body.focus();
    }));
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
          author.textContent = 'Deleted comment';
          actions.querySelector('.btn-outline-danger')?.remove();
          live.textContent = 'Comment deleted. Replies remain visible.';
        } catch (failure) { live.textContent = failure.message; alert(failure.message); }
      }, 'danger'));
    article.append(actions, slot);
    item.append(article);
    const children = node('ul', 'discussion-children');
    children.setAttribute('aria-label', 'Replies');
    item.append(children);
    load(children, comment.id);
    return item;
  }
  async function load(list, parentId, cursor) {
    const status = node('li', 'discussion-loading', parentId ? 'Loading replies…' : 'Loading discussion…');
    list.append(status);
    try {
      const pageUrl = new URL(url, location.origin);
      pageUrl.searchParams.set('limit', '20');
      if (parentId) pageUrl.searchParams.set('parentId', parentId);
      if (cursor) pageUrl.searchParams.set('cursor', cursor);
      const response = await fetch(pageUrl, { credentials: 'same-origin' });
      if (!response.ok) throw new Error(await message(response));
      if (response.redirected) throw new Error('Session expired. Sign in again.');
      const page = await response.json();
      status.remove();
      for (const comment of page.comments) list.append(render(comment));
      if (!parentId && !cursor && !page.comments.length)
        list.append(node('li', 'discussion-empty', 'No comments yet. Start the discussion.'));
      if (page.nextCursor) {
        const more = node('li', 'discussion-more');
        more.append(button(parentId ? 'Load more replies' : 'Load more comments', () => {
          more.remove();
          load(list, parentId, page.nextCursor);
        }));
        list.append(more);
      }
    } catch (failure) {
      status.className = 'discussion-error';
      status.textContent = failure.message + ' ';
      status.append(button('Retry', () => { status.remove(); load(list, parentId, cursor); }));
    } finally { if (!parentId) root.setAttribute('aria-busy', 'false'); }
  }
  function refresh() {
    root.replaceChildren();
    root.setAttribute('aria-busy', 'true');
    const list = node('ul', 'discussion-list');
    root.append(list);
    load(list);
  }
  file.addEventListener('change', () => {
    const selected = file.files?.[0];
    if (!selected) return clearImage();
    if (!['image/jpeg', 'image/png', 'image/gif', 'image/webp'].includes(selected.type) ||
        selected.size === 0 || selected.size > 5 * 1024 * 1024) {
      clearImage();
      error.textContent = 'Choose one JPEG, PNG, GIF, or WebP image under 5 MB.';
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
  form.querySelector('[data-clear-image]').addEventListener('click', () => { clearImage(); file.focus(); });
  form.querySelector('[data-cancel-reply]').addEventListener('click', () => { resetForm(); body.focus(); });
  form.addEventListener('submit', async event => {
    event.preventDefault();
    error.hidden = true;
    if (!body.value.trim() && !file.files?.length) {
      error.textContent = 'Write a comment or add an image.';
      error.hidden = false;
      return;
    }
    const submit = form.querySelector('[data-submit-comment]');
    submit.disabled = true;
    submit.textContent = 'Posting…';
    try {
      const response = await fetch(form.action, {
        method: 'POST', credentials: 'same-origin', body: new FormData(form)
      });
      if (!response.ok) throw new Error(await message(response));
      if (response.redirected) throw new Error('Session expired. Sign in again.');
      resetForm();
      refresh();
      live.textContent = 'Comment posted.';
      body.focus();
    } catch (failure) { error.textContent = failure.message; error.hidden = false; }
    finally {
      submit.disabled = false;
      submit.textContent = parent.value ? 'Post reply' : 'Post comment';
    }
  });
  window.addEventListener('pagehide', () => { if (previewUrl) URL.revokeObjectURL(previewUrl); });
  refresh();
})();
