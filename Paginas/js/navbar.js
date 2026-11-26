/**
 * navbar.js
 * Renderiza la barra de navegación superior de forma segura (sin innerHTML con datos externos)
 * y oculta/muestra opciones según el rol del usuario autenticado.
 */
document.addEventListener("DOMContentLoaded", () => {
    const contenedorNav = document.getElementById("navbarPrincipal");
    if (!contenedorNav) return;

    const sesion = window.AuthGuard ? window.AuthGuard.obtenerSesionActual() : null;
    const rol = sesion ? sesion.rol : null;

    // Definir enlaces del sistema y qué roles pueden verlos
    const enlaces = [
        { texto: "Catálogo", href: "catalogo.html", roles: ["Publico", "Cliente", "Empleado", "Admin"] },
        { texto: "Mi Carrito / Pedidos", href: "carrito.html", roles: ["Cliente"] },
        { texto: "Gestión de Productos", href: "index.html", roles: ["Admin", "Empleado"] },
        { texto: "Clientes", href: "clientes.html", roles: ["Admin", "Empleado"] },
        { texto: "Empleados y Roles", href: "empleados.html", roles: ["Admin"] }
    ];

    const nav = document.createElement("nav");
    nav.className = "navbar navbar-expand-lg navbar-dark bg-dark mb-3 shadow-sm";

    const container = document.createElement("div");
    container.className = "container";

    const brand = document.createElement("a");
    brand.className = "navbar-brand fw-bold";
    brand.href = "catalogo.html";
    brand.textContent = "Tienda Web";
    container.appendChild(brand);

    const ul = document.createElement("ul");
    ul.className = "navbar-nav me-auto mb-2 mb-lg-0 flex-row gap-3";

    const rolEfectivo = rol || "Publico";

    enlaces.forEach(item => {
        if (item.roles.includes(rolEfectivo)) {
            const li = document.createElement("li");
            li.className = "nav-item";

            const a = document.createElement("a");
            a.className = "nav-link";
            if (window.location.pathname.endsWith(item.href)) {
                a.classList.add("active", "fw-semibold");
            }
            a.href = item.href;
            a.textContent = item.texto;

            li.appendChild(a);
            ul.appendChild(li);
        }
    });

    container.appendChild(ul);

    // Zona derecha: Rol + Botón Login o Cerrar Sesión
    const zonaUsuario = document.createElement("div");
    zonaUsuario.className = "d-flex align-items-center gap-2";

    if (sesion) {
        const textoIdentidad = sesion.usuario
            ? (sesion.usuarioId ? `${sesion.usuario} (ID: ${sesion.usuarioId})` : sesion.usuario)
            : (sesion.usuarioId ? `ID: ${sesion.usuarioId}` : "");

        if (textoIdentidad) {
            const saludo = document.createElement("span");
            saludo.className = "text-light small fw-semibold me-1";
            saludo.textContent = `Hola, ${textoIdentidad}`;
            zonaUsuario.appendChild(saludo);
        }

        const badge = document.createElement("span");
        badge.className = "badge bg-info text-dark";
        badge.textContent = `Rol: ${sesion.rol}`;
        zonaUsuario.appendChild(badge);

        const btnSalir = document.createElement("button");
        btnSalir.type = "button";
        btnSalir.className = "btn btn-sm btn-outline-light";
        btnSalir.textContent = "Cerrar sesión";
        btnSalir.addEventListener("click", () => window.AuthGuard.cerrarSesion());
        zonaUsuario.appendChild(btnSalir);
    } else {
        const btnLogin = document.createElement("a");
        btnLogin.href = "login.html";
        btnLogin.className = "btn btn-sm btn-outline-light";
        btnLogin.textContent = "Iniciar sesión";
        zonaUsuario.appendChild(btnLogin);
    }

    container.appendChild(zonaUsuario);
    nav.appendChild(container);

    contenedorNav.replaceChildren(nav);
});