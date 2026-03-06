from __future__ import annotations

from pathlib import Path

import trimesh


THREE_JS_VIEWER = """<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>{title}</title>
  <style>
    body {{ margin: 0; font-family: Helvetica, Arial, sans-serif; background: linear-gradient(180deg, #6d6e73, #43454a); color: #f4f4f4; }}
    #meta {{ position: fixed; top: 16px; left: 16px; z-index: 10; padding: 12px 14px; background: rgba(0,0,0,0.45); border-radius: 12px; }}
    #viewer {{ width: 100vw; height: 100vh; }}
  </style>
</head>
<body>
  <div id="meta">
    <div><strong>{title}</strong></div>
    <div>Triangles: {triangles}</div>
    <div>Dimensions: {dimensions}</div>
  </div>
  <div id="viewer"></div>
  <script type="module">
    import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.164.1/build/three.module.js';
    import {{ OrbitControls }} from 'https://cdn.jsdelivr.net/npm/three@0.164.1/examples/jsm/controls/OrbitControls.js';
    import {{ GLTFLoader }} from 'https://cdn.jsdelivr.net/npm/three@0.164.1/examples/jsm/loaders/GLTFLoader.js';

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0x74777d);
    const camera = new THREE.PerspectiveCamera(45, window.innerWidth / window.innerHeight, 0.1, 100);
    camera.position.set(2.5, 2.0, 2.5);

    const renderer = new THREE.WebGLRenderer({{ antialias: true }});
    renderer.setSize(window.innerWidth, window.innerHeight);
    document.getElementById('viewer').appendChild(renderer.domElement);

    scene.add(new THREE.HemisphereLight(0xffffff, 0x222222, 1.3));
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.9);
    directionalLight.position.set(3, 6, 4);
    scene.add(directionalLight);

    const grid = new THREE.GridHelper(10, 20, 0xaaaaaa, 0x666666);
    scene.add(grid);

    const controls = new OrbitControls(camera, renderer.domElement);
    controls.target.set(0, 0.5, 0);
    controls.update();

    const loader = new GLTFLoader();
    loader.load('{model_name}', (gltf) => {{
      scene.add(gltf.scene);
    }});

    window.addEventListener('resize', () => {{
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
    }});

    function render() {{
      requestAnimationFrame(render);
      renderer.render(scene, camera);
    }}
    render();
  </script>
</body>
</html>
"""


class ViewerGenerator:
    def generate(self, glb_path: Path, output_html: Path) -> Path:
        mesh = trimesh.load(glb_path, force="mesh")
        if isinstance(mesh, trimesh.Scene):
            mesh = mesh.dump(concatenate=True)
        payload = THREE_JS_VIEWER.format(
            title=glb_path.stem,
            model_name=glb_path.name,
            triangles=len(mesh.faces),
            dimensions=", ".join(f"{value:.2f}m" for value in mesh.bounding_box.extents),
        )
        output_html.parent.mkdir(parents=True, exist_ok=True)
        output_html.write_text(payload)
        copied_glb = output_html.parent / glb_path.name
        if copied_glb != glb_path:
            copied_glb.write_bytes(glb_path.read_bytes())
        return output_html

    def generate_gallery(self, glb_paths: list[Path], output_html: Path) -> Path:
        cards = []
        for glb_path in glb_paths:
            cards.append(
                f"<article><h2>{glb_path.stem}</h2><p><a href=\"{glb_path.stem}.html\">Open viewer</a></p></article>"
            )
        html = (
            "<!doctype html><html><head><meta charset='utf-8'><title>DLTI Gallery</title>"
            "<style>body{font-family:Helvetica,Arial,sans-serif;background:#ece9e2;padding:24px;}main{display:grid;gap:16px;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));}article{background:#fff;padding:16px;border-radius:12px;box-shadow:0 6px 18px rgba(0,0,0,.08);}</style>"
            "</head><body><h1>DLTI Asset Gallery</h1><main>"
            + "".join(cards)
            + "</main></body></html>"
        )
        output_html.parent.mkdir(parents=True, exist_ok=True)
        output_html.write_text(html)
        return output_html
