/**
 * auth-guard.js
 * Control de acceso por URL ejecutado en el <head> antes de renderizar el body.
 */
(function () {
    function obtenerSesionActual() {
        const token = localStorage.getItem("token");
        if (!token) return null;

        try {
            const payloadBase64 = token.split(".")[1];
            const payloadJson = atob(payloadBase64.replace(/-/g, "+").replace(/_/g, "/"));
            const payload = JSON.parse(payloadJson);

            if (payload.exp && Date.now() >= payload.exp * 1000) {
                localStorage.removeItem("token");
                return null;
            }

            const rol =
                payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
                payload.role ||
                payload.Rol ||
                "";

            const usuario =
                payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ||
                payload.unique_name ||
                payload.sub ||
                "";

            return { token, rol, usuario, payload };
        } catch {
            localStorage.removeItem("token");
            return null;
        }
    }

    // Exponer utilidades globales para navbar.js y otros scripts
    window.AuthGuard = {
        obtenerSesionActual,
        cerrarSesion() {
            localStorage.removeItem("token");
            window.location.replace("login.html");
        },
        verificarRoles(rolesPermitidos) {
            const sesion = obtenerSesionActual();
            if (!sesion) {
                window.location.replace("login.html");
                return null;
            }
            if (Array.isArray(rolesPermitidos) && rolesPermitidos.length > 0) {
                if (!rolesPermitidos.includes(sesion.rol)) {
                    alert("No tienes permisos para acceder a esta sección.");
                    window.location.replace("catalogo.html");
                    return null;
                }
            }
            return sesion;
        }
    };

    // Validación automática mediante atributo data-roles en la etiqueta <script> del <head>
    // Ejemplo: <script src="js/auth-guard.js" data-roles="Admin,Empleado"></script>
    const scriptActual = document.currentScript;
    if (scriptActual && scriptActual.hasAttribute("data-roles")) {
        const attrRoles = scriptActual.getAttribute("data-roles").trim();
        if (attrRoles === "*") {
            window.AuthGuard.verificarRoles([]); // Cualquier usuario autenticado
        } else if (attrRoles.length > 0) {
            const listaRoles = attrRoles.split(",").map(r => r.trim());
            window.AuthGuard.verificarRoles(listaRoles);
        }
    }
})();