/**
 * seguridad-ui.js
 * Barrera 2 (Anti-XSS en Salida):
 * Garantiza que ningún dato proveniente de la API/BD se interprete como HTML.
 */
const SeguridadUI = {
    /**
     * Inserta texto de forma segura en un elemento existente usando textContent.
     */
    setText(elemento, valor, valorPorDefecto = "") {
        if (!elemento) return;
        const textoSeguro = (valor === null || valor === undefined) ? valorPorDefecto : String(valor);
        elemento.textContent = textoSeguro;
    },

    /**
     * Crea un elemento HTML (ej. <td>, <h5>, <p>, <option>) con texto plano seguro.
     */
    crearElementoTexto(etiqueta, valor, clases = "", valorPorDefecto = "") {
        const el = document.createElement(etiqueta);
        if (clases) el.className = clases;
        const textoSeguro = (valor === null || valor === undefined) ? valorPorDefecto : String(valor);
        el.appendChild(document.createTextNode(textoSeguro));
        return el;
    },

    /**
     * Limpia un contenedor (como <tbody> o un grid de cards) sin usar innerHTML = ""
     */
    limpiarContenedor(contenedor) {
        if (!contenedor) return;
        while (contenedor.firstChild) {
            contenedor.removeChild(contenedor.firstChild);
        }
    },

    /**
     * Helper para columnas de jQuery DataTables (si alguna vista lo utiliza)
     */
    renderDataTableTexto() {
        if (window.jQuery && $.fn.dataTable && $.fn.dataTable.render) {
            return $.fn.dataTable.render.text();
        }
        return function (data) {
            if (data === null || data === undefined) return "";
            const div = document.createElement("div");
            div.textContent = String(data);
            return div.textContent;
        };
    }
};