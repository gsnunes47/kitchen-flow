const API_BASE_URL = "http://localhost:5196";
const STORAGE_KEY = "kitchen-flow:cashier-orders";
// Mantém os pedidos criados antes do rename disponíveis no navegador.
const LEGACY_STORAGE_KEY = "pizza-flow:cashier-orders";

const meals = [
  { name: "Prato executivo", price: 3500 },
  { name: "Hambúrguer artesanal", price: 3200 },
  { name: "Massa ao molho", price: 3800 },
  { name: "Salada completa", price: 2800 },
];

const beverages = [
  { name: "Sem bebida", price: 0 },
  { name: "Coca-Cola", price: 800 },
  { name: "Guaraná", price: 700 },
  { name: "Água", price: 500 },
];

const orderForm = document.querySelector("#order-form");
const submitButton = document.querySelector("#submit-order");
const feedback = document.querySelector("#form-feedback");
const ordersList = document.querySelector("#orders-list");
const emptyState = document.querySelector("#empty-state");
const ordersCount = document.querySelector("#orders-count");
const toast = document.querySelector("#toast");
const toastMessage = document.querySelector("#toast-message");
const tabButtons = document.querySelectorAll("[data-tab]");
const tabPanels = document.querySelectorAll(".tab-panel");

let orders = loadOrders();
let toastTimeout;

function loadOrders() {
  try {
    const storedOrders =
      localStorage.getItem(STORAGE_KEY) ??
      localStorage.getItem(LEGACY_STORAGE_KEY);

    return (JSON.parse(storedOrders) ?? []).map((order) => ({
      ...order,
      meal: order.meal ?? order.pizza,
    }));
  } catch {
    return [];
  }
}

function saveOrders() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(orders));
}

function showTab(tabId) {
  tabButtons.forEach((button) => {
    const isActive = button.dataset.tab === tabId;
    button.classList.toggle("active", isActive);
    button.setAttribute("aria-selected", String(isActive));
  });

  tabPanels.forEach((panel) => {
    const isActive = panel.id === tabId;
    panel.classList.toggle("active", isActive);
    panel.hidden = !isActive;
  });
}

function setFeedback(message, type = "") {
  feedback.textContent = message;
  feedback.className = `feedback ${type}`.trim();
}

function showToast(message, type = "success") {
  window.clearTimeout(toastTimeout);
  toastMessage.textContent = message;
  toast.classList.toggle("error", type === "error");
  toast.querySelector(".toast-icon").textContent = type === "error" ? "!" : "✓";
  toast.classList.add("visible");

  toastTimeout = window.setTimeout(() => {
    toast.classList.remove("visible");
  }, 3000);
}

function setSubmitting(isSubmitting) {
  submitButton.disabled = isSubmitting;
  submitButton.firstChild.textContent = isSubmitting
    ? "Enviando... "
    : "Enviar pedido ";
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatDate(value) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatCurrency(valueInCents) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(valueInCents / 100);
}

function findProduct(name) {
  return [...meals, ...beverages].find((product) => product.name === name);
}

function buildItems(meal, beverage) {
  return [meal, beverage]
    .map((name) => findProduct(name))
    .filter((product) => product && product.price > 0)
    .map((product) => ({
      name: product.name,
      quantity: 1,
      price: product.price,
    }));
}

function getOrderItems(order) {
  return order.items ?? buildItems(order.meal, order.beverage);
}

function getOrderTotal(order) {
  return getOrderItems(order).reduce(
    (total, item) => total + item.quantity * item.price,
    0,
  );
}

function optionsMarkup(options, selected) {
  return options
    .map(
      (option) =>
        `<option value="${escapeHtml(option.name)}" ${option.name === selected ? "selected" : ""}>${escapeHtml(option.name)} — ${formatCurrency(option.price)}</option>`,
    )
    .join("");
}

function renderOrders() {
  ordersCount.textContent = String(orders.length);
  emptyState.hidden = orders.length > 0;

  ordersList.innerHTML = orders
    .map(
      (order) => `
        <article class="order-card" data-order-id="${escapeHtml(order.orderId)}">
          <div>
            <h3>${escapeHtml(order.customerName)}</h3>
            <p class="order-meta">
              <span>${escapeHtml(order.meal)}</span>
              <span>${escapeHtml(order.beverage)}</span>
              <span>${escapeHtml(order.phone ?? "Telefone não informado")}</span>
              <span>${formatDate(order.createdAt)}</span>
              <strong>${formatCurrency(getOrderTotal(order))}</strong>
            </p>
            <span class="event-tag">${escapeHtml(order.lastEvent ?? "OrderCreated")}</span>
          </div>
          <div class="order-actions">
            <button class="edit-button" type="button" data-edit-order>
              Alterar pedido
            </button>
            <button
              class="delivery-button"
              type="button"
              data-delivery-order
              ${order.lastEvent === "OrderOutToDelivery" ? "disabled" : ""}
            >
              ${order.lastEvent === "OrderOutToDelivery" ? "Em entrega" : "Saiu pra entregar"}
            </button>
          </div>
        </article>
      `,
    )
    .join("");
}

function createEditForm(order) {
  return `
    <form class="edit-form" data-edit-form>
      <label>
        Cliente
        <input name="customerName" value="${escapeHtml(order.customerName)}" maxlength="80" required />
      </label>
      <label>
        Telefone
        <input name="phone" type="tel" value="${escapeHtml(order.phone ?? "")}" maxlength="20" required />
      </label>
      <label>
        Prato
        <select name="meal">${optionsMarkup(meals, order.meal)}</select>
      </label>
      <label>
        Bebida
        <select name="beverage">${optionsMarkup(beverages, order.beverage)}</select>
      </label>
      <div class="edit-actions">
        <button class="primary-button" type="submit">Salvar</button>
        <button class="cancel-button" type="button" data-cancel-edit>Cancelar</button>
      </div>
      <p class="feedback full-width" data-edit-feedback role="status"></p>
    </form>
  `;
}

async function parseResponse(response) {
  const text = await response.text();

  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

orderForm.addEventListener("submit", async (event) => {
  event.preventDefault();

  const formData = new FormData(orderForm);
  const meal = formData.get("meal");
  const beverage = formData.get("beverage");
  const request = {
    customerName: formData.get("customerName").trim(),
    phone: formData.get("phone").trim(),
    items: buildItems(meal, beverage),
  };

  setFeedback("");
  setSubmitting(true);

  try {
    const response = await fetch(`${API_BASE_URL}/orders`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });

    const responseBody = await parseResponse(response);

    if (!response.ok) {
      throw new Error(
        typeof responseBody === "string"
          ? responseBody
          : `A API respondeu com status ${response.status}.`,
      );
    }

    const createdOrder = {
      customerName: request.customerName,
      phone: request.phone,
      meal,
      beverage,
      ...responseBody,
      orderId: responseBody?.orderId ?? crypto.randomUUID(),
      createdAt: responseBody?.createdAt ?? new Date().toISOString(),
      lastEvent: "OrderCreated",
    };

    orders.unshift(createdOrder);
    saveOrders();
    renderOrders();
    orderForm.reset();
    showToast("Pedido criado");
  } catch (error) {
    setFeedback(
      `Não foi possível enviar: ${error.message} Verifique a API e o CORS.`,
      "error",
    );
  } finally {
    setSubmitting(false);
  }
});

ordersList.addEventListener("click", async (event) => {
  const card = event.target.closest("[data-order-id]");

  if (!card) {
    return;
  }

  if (event.target.closest("[data-cancel-edit]")) {
    card.querySelector("[data-edit-form]")?.remove();
    return;
  }

  const deliveryButton = event.target.closest("[data-delivery-order]");

  if (deliveryButton) {
    const orderId = card.dataset.orderId;
    deliveryButton.disabled = true;
    deliveryButton.textContent = "Enviando...";

    try {
      const response = await fetch(`${API_BASE_URL}/orders/delivery/${orderId}`, {
        method: "POST",
      });

      const responseBody = await parseResponse(response);

      if (!response.ok) {
        throw new Error(
          typeof responseBody === "string"
            ? responseBody
            : `A API respondeu com status ${response.status}.`,
        );
      }

      orders = orders.map((order) =>
        order.orderId === orderId
          ? {
              ...order,
              ...(responseBody ?? {}),
              lastEvent: "OrderOutToDelivery",
              outToDeliveryAt: new Date().toISOString(),
            }
          : order,
      );

      saveOrders();
      renderOrders();
      showToast("Pedido saiu para entrega");
    } catch (error) {
      deliveryButton.disabled = false;
      deliveryButton.textContent = "Saiu pra entregar";
      showToast(`Não foi possível enviar: ${error.message}`, "error");
    }

    return;
  }

  if (!event.target.closest("[data-edit-order]")) {
    return;
  }

  document.querySelector("[data-edit-form]")?.remove();

  const order = orders.find((item) => item.orderId === card.dataset.orderId);

  if (order) {
    card.insertAdjacentHTML("beforeend", createEditForm(order));
  }
});

ordersList.addEventListener("submit", async (event) => {
  const editForm = event.target.closest("[data-edit-form]");

  if (!editForm) {
    return;
  }

  event.preventDefault();

  const card = editForm.closest("[data-order-id]");
  const orderId = card.dataset.orderId;
  const formData = new FormData(editForm);
  const meal = formData.get("meal");
  const beverage = formData.get("beverage");
  const editFeedback = editForm.querySelector("[data-edit-feedback]");
  const saveButton = editForm.querySelector("button[type='submit']");
  const request = {
    customerName: formData.get("customerName").trim(),
    phone: formData.get("phone").trim(),
    items: buildItems(meal, beverage),
  };

  saveButton.disabled = true;
  editFeedback.textContent = "Salvando alteração...";
  editFeedback.className = "feedback full-width";

  try {
    const response = await fetch(`${API_BASE_URL}/orders/${orderId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });

    const responseBody = await parseResponse(response);

    if (!response.ok) {
      throw new Error(
        typeof responseBody === "string"
          ? responseBody
          : `A API respondeu com status ${response.status}.`,
      );
    }

    orders = orders.map((order) =>
      order.orderId === orderId
        ? {
            ...order,
            ...request,
            meal,
            beverage,
            ...(responseBody ?? {}),
            lastEvent: "OrderUpdated",
            updatedAt: new Date().toISOString(),
          }
        : order,
    );

    saveOrders();
    renderOrders();
    showToast("Pedido alterado");
  } catch (error) {
    editFeedback.textContent = `Não foi possível alterar: ${error.message}`;
    editFeedback.className = "feedback error full-width";
    saveButton.disabled = false;
  }
});

tabButtons.forEach((button) => {
  button.addEventListener("click", () => showTab(button.dataset.tab));
});

document.querySelector("[data-go-to-create]").addEventListener("click", () => {
  showTab("novo-pedido");
  document.querySelector("#customer-name").focus();
});

renderOrders();
