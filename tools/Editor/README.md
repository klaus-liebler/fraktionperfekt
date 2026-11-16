# TOAST UI Editor – lokales Bündel

`src/FraktionPerfekt.Web/wwwroot/lib/toastui-editor/toastui-editor-all.min.js` ist ein mit esbuild gebautes Bündel aus
`@toast-ui/editor@3.2.2` (MIT) samt ProseMirror, DOMPurify und deutscher Oberfläche (i18n de-DE). Das npm-Paket bringt kein
fertiges Gesamtbündel mit; kein CDN, Version fest. Lizenzen aller enthaltenen Pakete: `LICENSE.txt` daneben.

Neu bauen (Node.js nötig):

    npm init -y
    npm install @toast-ui/editor@3.2.2 esbuild@0.24.2
    npx esbuild eintrag.js --bundle --minify --format=iife --legal-comments=eof --outfile=toastui-editor-all.min.js
    cp node_modules/@toast-ui/editor/dist/toastui-editor.css .

Einbindung: `App.razor` (Skript und CSS), `wwwroot/editor.js` (Start, Bild-Hook), `Components/Bausteine/MarkdownEditor.razor`.
