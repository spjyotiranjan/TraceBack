#!/usr/bin/env python3
"""Export only approved, visible route-background vector nodes from the project archive."""
import argparse
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
# In order: desktop/light, phone/light, desktop/dark, phone/dark.
ROUTES = {
    'home-guest': [(47316, 47326), (47354, 47364), (47388, 47398), (47426, 47436)],
    'home-member': [(47335, 47345), (47373, 47381), (47407, 47417), (47445, 47453)],
    'auth': [(5294, 5299), (5305, 5310), (5316, 5321), (5327, 5332)],
    'browse': [(34506, 34583), (34514, 34592), (34522, 34601), (34530, 34610)],
    'details': [(35258, 35267), (35275, 35284), (35292, 35301), (35309, 35318)],
    'editor': [(34538, 34619), (34549, 34631), (34560, 34643), (34571, 34655)],
    'dashboard': [(37111, 37118), (37124, 37131), (37137, 37144), (37150, 37157)],
    'denied': [(45146,), (45217,), (45285,), (45364,)],
    'not-found': [(45445,), (45514,), (45580,), (45657,)],
    'error': [(45736,), (45843,), (45949,), (46072,)],
    'privacy': [(47842,), (47942,), (48041,), (48157,)],
}


def num(value):
    return f'{value:.2f}'.rstrip('0').rstrip('.') if abs(value) >= .005 else '0'


def coords(*values):
    return ' '.join(map(num, values))


def rgb(color):
    return '#' + ''.join(f'{round(color[channel] * 255):02x}' for channel in 'rgb')


def path_data(network):
    """The allowlisted vector networks are strokes, not filled regions."""
    vertices = network['vertices']
    segments = network['segments']
    subpath_start = None
    result = []
    previous = None
    for segment in segments:
        a, b = segment['a'], segment['b']
        start, end = vertices[a], vertices[b]
        x1, y1 = start['x'], start['y']
        x2, y2 = end['x'], end['y']
        ta, tb = segment['ta'], segment['tb']
        if a != previous:
            result.append('M' + coords(x1, y1))
            subpath_start = a
        if ta == tb == [0, 0]:
            result.append('L' + coords(x2, y2))
        else:
            result.append('C' + coords(x1 + ta[0], y1 + ta[1], x2 + tb[0], y2 + tb[1], x2, y2))
        # Use a joined stroke on simple closed subpaths.
        if b == subpath_start:
            result.append('Z')
            previous = None
        else:
            previous = b
    return ''.join(result)


def paint(shape, field):
    paints = [p for p in shape.get(field, []) if p.get('visible', True)]
    if not paints:
        return None
    if len(paints) != 1 or paints[0]['type'] not in ('solid', 'gradient'):
        raise ValueError(f'Unexpected paint ({field}) on {shape["id"]}')
    return paints[0]


def vector_svg(nodes, node_id):
    node = nodes[str(node_id)]
    defs, shapes = {}, []
    for child_id in node['children']:
        child = nodes[str(child_id)]
        if not child.get('visible', True):
            continue
        fill, stroke = paint(child, 'fills'), paint(child, 'strokes')
        style = child.get('stroke', {})
        attrs = []
        geometry = child['type']
        if geometry == 'ellipse':
            rx, ry = child['width'] / 2, child['height'] / 2
            inset = style.get('weight', 1) / 2 if stroke and style.get('align') == 'inside' else 0
            attrs.extend(f'{key}="{num(value)}"' for key, value in
                         [('cx', rx), ('cy', ry), ('rx', rx-inset), ('ry', ry-inset)])
        elif geometry == 'vector' and fill is None:
            geometry = 'path'
            attrs.append(f'd="{path_data(child["network"])}"')
        else:
            raise ValueError(f'Unexpected geometry on {child_id}')

        if fill and fill['type'] == 'gradient':
            if (fill['kind'], fill['from'], fill['to'], fill['width']) != ('radial', [.5,.5], [.5,1], [1,.5]):
                raise ValueError(f'Unexpected gradient geometry on {child_id}')
            stops = ''.join(f'<stop offset="{num(stop["position"])}" stop-color="{rgb(stop["color"])}" '
                            f'stop-opacity="{num(stop["color"]["a"]*fill["opacity"])}"/>' for stop in fill['stops'])
            gradient = defs.setdefault(stops, f'g{len(defs)}')
            attrs.append(f'fill="url(#{gradient})"')
        elif fill:
            attrs += [f'fill="{rgb(fill["color"])}"', f'fill-opacity="{num(fill["color"]["a"]*fill["opacity"])}"']
        if stroke:
            if stroke['type'] != 'solid':
                raise ValueError(f'Unexpected stroke on {child_id}')
            attrs += [f'stroke="{rgb(stroke["color"])}"', f'stroke-width="{num(style.get("weight", 1))}"']
            opacity = stroke['color']['a'] * stroke['opacity']
            if opacity != 1:
                attrs.append(f'stroke-opacity="{num(opacity)}"')
            if style.get('cap_start') == style.get('cap_end') == 'round':
                attrs.append('stroke-linecap="round"')
            if style.get('join') == 'round':
                attrs.append('stroke-linejoin="round"')
            if style.get('dashes'):
                attrs.append(f'stroke-dasharray="{coords(*style["dashes"])}"')
        if child.get('opacity', 1) != 1:
            attrs.append(f'opacity="{num(child["opacity"])}"')
        transform = child.get('transform', [1, 0, 0, 1, 0, 0])
        if transform[:4] == [1, 0, 0, 1]:
            if transform[4:] != [0, 0]:
                attrs.append(f'transform="translate({coords(*transform[4:])})"')
        else:
            attrs.append(f'transform="matrix({coords(*transform)})"')
        # Auth radial fields already feather to transparent; skip their extra layer blur.
        shapes.append(f'<{geometry} {" ".join(attrs)}/>')

    gradients = ''.join(f'<radialGradient id="{key}">{stops}</radialGradient>' for stops, key in defs.items())
    size = coords(node['width'], node['height'])
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {size}" fill="none">\n'
            f'<defs>{gradients}</defs>\n' + '\n'.join(shapes) + '\n</svg>\n')


def export(nodes):
    files = {}
    css = ['/* Generated by scripts/export-route-art.py. */']
    for device, indent, pairs in [('desktop', '', (0, 2)), ('phone', '  ', (1, 3))]:
        if indent:
            css.append('@media (max-width: 599px) {')
        for route, variants in ROUTES.items():
            css.append(f'{indent}[data-route-art="{route}"] {{')
            sizes = {(nodes[str(node)]['width'], nodes[str(node)]['height'])
                     for index in pairs for node in variants[index]}
            if len(sizes) != 1:
                raise ValueError(f'Phase or theme dimensions differ for {route}/{device}')
            width, height = sizes.pop()
            css.append(f'{indent}  --tb-route-art-ratio: {num(width)} / {num(height)};')
            for phase_index, phase in enumerate('ab'[:len(variants[0])]):
                urls = []
                for mode, variant_index in zip(('light', 'dark'), pairs):
                    name = f'{route}-{device}-{mode}-{phase}.svg'
                    svg = vector_svg(nodes, variants[variant_index][phase_index])
                    # Reuse an existing file only when the entire exported pose matches.
                    name = next((key for key, value in files.items() if value == svg), name)
                    files[name] = svg
                    urls.append(f'var(--tb-{mode}, url("../art/routes/{name}"))')
                css.append(f'{indent}  --tb-route-art-{phase}: {" ".join(urls)};')
            css.append(f'{indent}}}')
        if indent:
            css.append('}')
    return files, '\n'.join(css) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Verify committed SVG and CSS bytes without writing.')
    args = parser.parse_args()
    with zipfile.ZipFile(ROOT / 'design/TraceBack-Campus-Signal.figcraft') as archive:
        nodes = json.loads(archive.read('document.json'))['nodes']
    files, css = export(nodes)
    web = ROOT / 'src/TraceBack/wwwroot'
    art = web / 'art/routes'
    outputs = {art / name: text for name, text in files.items()}
    outputs[web / 'css/route-art-assets.css'] = css
    stale = set(art.glob('*.svg')) - outputs.keys()
    if stale:
        parser.error('Remove obsolete generated SVGs first: ' + ', '.join(sorted(p.name for p in stale)))
    for path, data in outputs.items():
        if args.check:
            if not path.exists() or path.read_text(encoding='utf-8') != data:
                parser.error(f'Generated output differs: {path.relative_to(ROOT)}')
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(data, encoding='utf-8', newline='\n')
    print(f'{len(files)} vector poses, {sum(map(len, files.values())):,} SVG bytes')


if __name__ == '__main__':
    main()
