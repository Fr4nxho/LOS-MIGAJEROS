const items = [
    {
        id: 1,
        nombre: "Cuaderno A4",
        tipo: "Producto",
        precio: 8.50,
        stock: 20
    },
    {
        id: 2,
        nombre: "Impresión",
        tipo: "Servicio",
        precio: 0.50
    },
    {
        id: 3,
        nombre: "Fotocopia",
        tipo: "Servicio",
        precio: 0.20
    }
];

let detalleVenta = [];

const listaItems = document.getElementById("listaItems");
const detalleVentaBody = document.getElementById("detalleVenta");
const buscarItemInput = document.getElementById("buscarItem");
const btnBuscar = document.getElementById("btnBuscar");
const descuentoInput = document.getElementById("descuento");
const subtotalElement = document.getElementById("subtotal");
const totalElement = document.getElementById("total");
const medioPagoSelect = document.getElementById("medioPago");
const btnRegistrarVenta = document.getElementById("btnRegistrarVenta");
const mensaje = document.getElementById("mensaje");


function mostrarItems(lista) {
    listaItems.innerHTML = "";

    if (lista.length === 0) {
        listaItems.innerHTML = "<p>No se encontraron productos o servicios.</p>";
        return;
    }

    lista.forEach(item => {
        const articulo = document.createElement("article");

        let informacionStock = "";

        if (item.tipo === "Producto") {
            informacionStock = `<p>Stock: ${item.stock}</p>`;
        }

        articulo.innerHTML = `
            <h3>${item.nombre}</h3>
            <p>Tipo: ${item.tipo}</p>
            <p>Precio: S/ ${item.precio.toFixed(2)}</p>
            ${informacionStock}
            <button type="button" onclick="agregarItem(${item.id})">
                Agregar
            </button>
        `;

        listaItems.appendChild(articulo);
    });
}


function buscarItems() {
    const textoBusqueda = buscarItemInput.value
        .trim()
        .toLowerCase();

    const resultados = items.filter(item =>
        item.nombre.toLowerCase().includes(textoBusqueda)
    );

    mostrarItems(resultados);
}


function agregarItem(id) {
    const item = items.find(item => item.id === id);

    if (!item) {
        return;
    }

    const itemExistente = detalleVenta.find(
        detalle => detalle.id === id
    );

    if (itemExistente) {
        if (
            item.tipo === "Producto" &&
            itemExistente.cantidad >= item.stock
        ) {
            mostrarMensaje(
                "No existe stock suficiente para agregar más unidades.",
                false
            );

            return;
        }

        itemExistente.cantidad++;
    } else {
        if (item.tipo === "Producto" && item.stock <= 0) {
            mostrarMensaje(
                "El producto seleccionado no tiene stock disponible.",
                false
            );

            return;
        }

        detalleVenta.push({
            id: item.id,
            nombre: item.nombre,
            tipo: item.tipo,
            precio: item.precio,
            cantidad: 1,
            stock: item.stock ?? null
        });
    }

    actualizarDetalleVenta();
}


function actualizarDetalleVenta() {
    detalleVentaBody.innerHTML = "";

    detalleVenta.forEach(item => {
        const fila = document.createElement("tr");

        const subtotalItem = item.precio * item.cantidad;

        fila.innerHTML = `
            <td>${item.nombre}</td>

            <td>${item.tipo}</td>

            <td>
                S/ ${item.precio.toFixed(2)}
            </td>

            <td>
                <input
                    type="number"
                    min="1"
                    value="${item.cantidad}"
                    onchange="cambiarCantidad(${item.id}, this.value)"
                >
            </td>

            <td>
                S/ ${subtotalItem.toFixed(2)}
            </td>

            <td>
                <button
                    type="button"
                    onclick="eliminarItem(${item.id})"
                >
                    Eliminar
                </button>
            </td>
        `;

        detalleVentaBody.appendChild(fila);
    });

    calcularTotales();
}


function cambiarCantidad(id, nuevaCantidad) {
    nuevaCantidad = parseInt(nuevaCantidad);

    const itemDetalle = detalleVenta.find(
        item => item.id === id
    );

    if (!itemDetalle) {
        return;
    }

    if (isNaN(nuevaCantidad) || nuevaCantidad < 1) {
        nuevaCantidad = 1;
    }

    if (
        itemDetalle.tipo === "Producto" &&
        nuevaCantidad > itemDetalle.stock
    ) {
        mostrarMensaje(
            `Solo hay ${itemDetalle.stock} unidades disponibles.`,
            false
        );

        nuevaCantidad = itemDetalle.stock;
    }

    itemDetalle.cantidad = nuevaCantidad;

    actualizarDetalleVenta();
}


function eliminarItem(id) {
    detalleVenta = detalleVenta.filter(
        item => item.id !== id
    );

    actualizarDetalleVenta();
}


function calcularTotales() {
    const subtotal = detalleVenta.reduce(
        (acumulador, item) =>
            acumulador + item.precio * item.cantidad,
        0
    );

    let descuento = parseFloat(descuentoInput.value);

    if (isNaN(descuento) || descuento < 0) {
        descuento = 0;
    }

    if (descuento > subtotal) {
        descuento = subtotal;
        descuentoInput.value = descuento.toFixed(2);
    }

    const total = subtotal - descuento;

    subtotalElement.textContent =
        `S/ ${subtotal.toFixed(2)}`;

    totalElement.textContent =
        `S/ ${total.toFixed(2)}`;
}


function registrarVenta() {
    if (detalleVenta.length === 0) {
        mostrarMensaje(
            "Debe agregar al menos un producto o servicio.",
            false
        );

        return;
    }

    if (medioPagoSelect.value === "") {
        mostrarMensaje(
            "Debe seleccionar un medio de pago.",
            false
        );

        return;
    }

    detalleVenta.forEach(detalle => {
        if (detalle.tipo === "Producto") {
            const producto = items.find(
                item => item.id === detalle.id
            );

            producto.stock -= detalle.cantidad;
        }
    });

    mostrarMensaje(
        "Venta registrada correctamente.",
        true
    );

    detalleVenta = [];

    descuentoInput.value = 0;
    medioPagoSelect.value = "";

    actualizarDetalleVenta();
    mostrarItems(items);
}


function mostrarMensaje(texto, correcto) {
    mensaje.textContent = texto;

    if (correcto) {
        mensaje.style.color = "green";
    } else {
        mensaje.style.color = "red";
    }
}


btnBuscar.addEventListener(
    "click",
    buscarItems
);


buscarItemInput.addEventListener(
    "input",
    buscarItems
);


descuentoInput.addEventListener(
    "input",
    calcularTotales
);


btnRegistrarVenta.addEventListener(
    "click",
    registrarVenta
);


mostrarItems(items);