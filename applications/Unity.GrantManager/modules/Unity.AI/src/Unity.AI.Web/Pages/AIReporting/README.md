# AI Reporting Page

`Index.*` currently uses the iframe-hosted AI Reporting app.

The converted native Razor implementation is parked beside it as:

- `Index.native.cshtml.disabled`
- `Index.native.cshtml.cs.disabled`
- `Index.native.css.disabled`
- `Index.native.js.disabled`

To switch back to the native implementation:

1. Replace `Index.cshtml` with `Index.native.cshtml.disabled`.
2. Replace `Index.cshtml.cs` with `Index.native.cshtml.cs.disabled`.
3. Replace `Index.js` with `Index.native.js.disabled`.
4. Rename `Index.native.css.disabled` to `Index.css`.
5. Build `Unity.AI.Web` and `Unity.GrantManager.Web`.
