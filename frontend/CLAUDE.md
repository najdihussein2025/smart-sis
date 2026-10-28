# frontend/ — read before editing

@../docs/rules/frontend.md

- Routes are in `src/app/router.tsx`. Screens from later phases use `soon([...])` → replace the entry
  with the real page when the feature is built.
- Design tokens live in `src/index.css` (`@theme`). Use the token classes, never hex.
- `npm run build` must pass (it type-checks).
