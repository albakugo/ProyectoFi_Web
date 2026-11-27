const API_BASE_URL = "https://localhost:7214/api";

// --- Utilidades Seguras (Mensajes y Anti-XSS) ---
function mostrarMensaje(contenedorId, texto, tipo = "info") {
    const contenedor = document.getElementById(contenedorId);
    if (!contenedor) return;

    contenedor.className = `alert alert-${tipo} mt-3`;
    contenedor.textContent = texto; // textContent evita ejecucion de HTML/XSS
    contenedor.classList.remove("d-none");
}

function crearCeldaTexto(texto) {
    const td = document.createElement("td");
    td.textContent = texto ?? "";
    return td;
}

function obtenerHeadersAuth(token) {
    return {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
    };
}

// --- Modulo de Empleados y Roles (Exclusivo Admin) ---
async function cargarEmpleados(token) {
    const tbody = document.getElementById("tablaEmpleadosBody");
    if (!tbody) return;

    try {
        const respuesta = await fetch(`${API_BASE_URL}/Employees`, {
            method: "GET",
            headers: obtenerHeadersAuth(token)
        });

        const datos = await respuesta.json();

        if (!respuesta.ok) {
            mostrarMensaje("mensajeEmpleados", datos.mensaje || "No se pudo cargar la lista de empleados.", "danger");
            return;
        }

        tbody.replaceChildren(); // Limpia la tabla de forma segura

        if (datos.length === 0) {
            const tr = document.createElement("tr");
            const td = document.createElement("td");
            td.colSpan = 8;
            td.className = "text-center text-muted";
            td.textContent = "No hay empleados activos registrados.";
            tr.appendChild(td);
            tbody.appendChild(tr);
            return;
        }

        datos.forEach(emp => {
            const tr = document.createElement("tr");
            tr.appendChild(crearCeldaTexto(emp.employeeId));
            tr.appendChild(crearCeldaTexto(emp.nombreCompleto));
            tr.appendChild(crearCeldaTexto(emp.nombreUsuario));
            tr.appendChild(crearCeldaTexto(emp.rol));
            tr.appendChild(crearCeldaTexto(emp.title || "N/A"));
            tr.appendChild(crearCeldaTexto(emp.emailEnmascarado));
            tr.appendChild(crearCeldaTexto(emp.telefonoEnmascarado));

            const tdAccion = document.createElement("td");
            const btnBaja = document.createElement("button");
            btnBaja.type = "button";
            btnBaja.className = "btn btn-sm btn-outline-danger";
            btnBaja.textContent = "Dar de baja";
            btnBaja.addEventListener("click", () => darBajaEmpleado(emp.employeeId, emp.nombreCompleto, token));

            tdAccion.appendChild(btnBaja);
            tr.appendChild(tdAccion);
            tbody.appendChild(tr);
        });
    } catch {
        mostrarMensaje("mensajeEmpleados", "Error de conexión con el servidor al consultar empleados.", "danger");
    }
}

async function registrarEmpleado(evento, token) {
    evento.preventDefault(); // Bloquea envio tradicional del formulario

    const payload = {
        nombreUsuario: document.getElementById("empUsuario").value.trim(),
        email: document.getElementById("empEmail").value.trim(),
        password: document.getElementById("empPassword").value,
        rol: document.getElementById("empRol").value,
        firstName: document.getElementById("empNombre").value.trim(),
        lastName: document.getElementById("empApellido").value.trim(),
        title: document.getElementById("empPuesto").value.trim(),
        city: document.getElementById("empCiudad").value.trim(),
        homePhone: document.getElementById("empTelefono").value.trim()
    };

    try {
        const respuesta = await fetch(`${API_BASE_URL}/Employees`, {
            method: "POST",
            headers: obtenerHeadersAuth(token),
            body: JSON.stringify(payload)
        });

        const datos = await respuesta.json();

        if (!respuesta.ok) {
            mostrarMensaje("mensajeEmpleados", datos.mensaje || "No se pudo registrar el empleado.", "warning");
            return;
        }

        mostrarMensaje("mensajeEmpleados", datos.mensaje, "success");
        document.getElementById("formRegistrarEmpleado").reset();
        await cargarEmpleados(token);
    } catch {
        mostrarMensaje("mensajeEmpleados", "Error de comunicación al registrar el empleado.", "danger");
    }
}

async function darBajaEmpleado(employeeId, nombreCompleto, token) {
    const confirmar = confirm(`¿Seguro que deseas dar de baja lógica a "${nombreCompleto}"?`);
    if (!confirmar) return;

    try {
        const respuesta = await fetch(`${API_BASE_URL}/Employees/${employeeId}`, {
            method: "DELETE",
            headers: obtenerHeadersAuth(token)
        });

        const datos = await respuesta.json();

        if (!respuesta.ok) {
            mostrarMensaje("mensajeEmpleados", datos.mensaje || "No se pudo dar de baja al empleado.", "warning");
            return;
        }

        mostrarMensaje("mensajeEmpleados", datos.mensaje, "success");
        await cargarEmpleados(token);
    } catch {
        mostrarMensaje("mensajeEmpleados", "Error de conexión al intentar dar de baja al empleado.", "danger");
    }
}

// --- Modulo de Clientes (Vista inicial + Busqueda segura + Baja Logica) ---
function renderizarTablaClientes(datos, token, recargarCallback) {
    const tbody = document.getElementById("tablaClientesBody");
    if (!tbody) return;

    tbody.replaceChildren();

    if (!datos || datos.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");
        td.colSpan = 7;
        td.className = "text-center text-muted";
        td.textContent = "No se encontraron clientes activos.";
        tr.appendChild(td);
        tbody.appendChild(tr);
        return;
    }

    datos.forEach(cli => {
        const tr = document.createElement("tr");
        tr.appendChild(crearCeldaTexto(cli.customerId));
        tr.appendChild(crearCeldaTexto(cli.companyName));
        tr.appendChild(crearCeldaTexto(cli.contactName));
        tr.appendChild(crearCeldaTexto(cli.city || "N/A"));
        tr.appendChild(crearCeldaTexto(cli.emailEnmascarado));
        tr.appendChild(crearCeldaTexto(cli.telefonoEnmascarado));

        const tdAccion = document.createElement("td");
        const btnBaja = document.createElement("button");
        btnBaja.type = "button";
        btnBaja.className = "btn btn-sm btn-outline-danger";
        btnBaja.textContent = "Baja lógica";
        btnBaja.addEventListener("click", () => darBajaCliente(cli.customerId, cli.companyName, token, recargarCallback));

        tdAccion.appendChild(btnBaja);
        tr.appendChild(tdAccion);
        tbody.appendChild(tr);
    });
}

async function cargarClientesIniciales(token) {
    try {
        const respuesta = await fetch(`${API_BASE_URL}/Customers/recientes`, {
            method: "GET",
            headers: obtenerHeadersAuth(token)
        });

        if (respuesta.status === 401 && window.AuthGuard) {
            window.AuthGuard.cerrarSesion();
            return;
        }

        const datos = await respuesta.json();
        if (!respuesta.ok) {
            mostrarMensaje("mensajeClientes", datos.mensaje || "No se pudo cargar la lista de clientes.", "danger");
            return;
        }

        renderizarTablaClientes(datos, token, () => cargarClientesIniciales(token));
    } catch {
        mostrarMensaje("mensajeClientes", "Error de conexión al cargar los clientes.", "danger");
    }
}

async function buscarClientes(evento, token) {
    if (evento) evento.preventDefault();

    const criterio = document.getElementById("criterioCliente").value.trim();

    if (criterio.length === 0) {
        await cargarClientesIniciales(token);
        return;
    }

    if (criterio.length < 2) {
        mostrarMensaje("mensajeClientes", "Ingresa al menos 2 caracteres para filtrar o deja vacío para ver recientes.", "warning");
        return;
    }

    try {
        const respuesta = await fetch(`${API_BASE_URL}/Customers/buscar`, {
            method: "POST",
            headers: obtenerHeadersAuth(token),
            body: JSON.stringify({ criterio })
        });

        if (respuesta.status === 401 && window.AuthGuard) {
            window.AuthGuard.cerrarSesion();
            return;
        }

        const datos = await respuesta.json();

        if (!respuesta.ok) {
            mostrarMensaje("mensajeClientes", datos.mensaje || "No se pudo realizar la búsqueda.", "danger");
            return;
        }

        renderizarTablaClientes(datos, token, () => buscarClientes(null, token));
    } catch {
        mostrarMensaje("mensajeClientes", "Error de conexión al buscar clientes.", "danger");
    }
}

async function darBajaCliente(customerId, companyName, token, recargarCallback) {
    const confirmar = confirm(`¿Seguro que deseas aplicar baja lógica al cliente "${companyName}" (${customerId})?`);
    if (!confirmar) return;

    try {
        const respuesta = await fetch(`${API_BASE_URL}/Customers/${encodeURIComponent(customerId)}`, {
            method: "DELETE",
            headers: obtenerHeadersAuth(token)
        });

        const datos = await respuesta.json();

        if (!respuesta.ok) {
            mostrarMensaje("mensajeClientes", datos.mensaje || "No se pudo dar de baja al cliente.", "warning");
            return;
        }

        mostrarMensaje("mensajeClientes", datos.mensaje, "success");
        if (typeof recargarCallback === "function") {
            await recargarCallback();
        }
    } catch {
        mostrarMensaje("mensajeClientes", "Error de conexión al intentar dar de baja al cliente.", "danger");
    }
}