const API_BASE_URL = "http://localhost:5196";
const SLOT_COUNT = 6;

const ordersGrid = document.querySelector("#orders-grid");
const queueCount = document.querySelector("#queue-count");
const panelStatus = document.querySelector("#panel-status");
const toast = document.querySelector("#toast");

let orders = [];
let selectedOrderId = null;
let toastTimeout;

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatCurrency(valueInCents) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(valueInCents / 100);
}

function formatTime(value) {
  return new Intl.DateTimeFormat("pt-BR", {
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function getItemValue(item, property) {
  return item[property] ?? item[property[0].toUpperCase() + property.slice(1)];
}

function getOrderTotal(order) {
  return order.items.reduce((total, item) => {
    const quantity = Number(getItemValue(item, "quantity") ?? 0);
    const price = Number(getItemValue(item, "price") ?? 0);
    return total + quantity * price;
  }, 0);
}

function getOrderNumber(order) {
  return order.id.slice(0, 8).toUpperCase();
}

function showToast(message, type = "success") {
  window.clearTimeout(toastTimeout);
  toast.textContent = message;
  toast.className = `toast visible ${type === "error" ? "error" : ""}`.trim();
  toastTimeout = window.setTimeout(() => toast.classList.remove("visible"), 3000);
}

function orderMarkup(order) {
  if (selectedOrderId === order.id) {
    return `
      <article class="order-slot finish-slot" data-order-id="${escapeHtml(order.id)}">
        <button class="finish-button" type="button" data-finish-order>Finalizar pedido</button>
      </article>`;
  }

  const items = order.items.map((item) => {
    const name = getItemValue(item, "name");
    const quantity = getItemValue(item, "quantity");
    return `<li><span class="item-quantity">${escapeHtml(quantity)}×</span><span>${escapeHtml(name)}</span></li>`;
  }).join("");

  return `
    <article class="order-slot order-card" data-order-id="${escapeHtml(order.id)}" tabindex="0">
      <header class="order-header">
        <div><h2>${escapeHtml(order.customerName)}</h2><p>${escapeHtml(order.phone)}</p></div>
        <span class="order-number">#${getOrderNumber(order)}</span>
      </header>
      <ul class="items-list">${items}</ul>
      <footer class="order-footer">
        <span class="order-time">Recebido às ${formatTime(order.createdAt)}</span>
        <strong class="order-total">${formatCurrency(getOrderTotal(order))}</strong>
      </footer>
    </article>`;
}

function renderOrders() {
  queueCount.textContent = String(orders.length);
  const visibleOrders = orders.slice(0, SLOT_COUNT);
  const emptySlots = Array.from(
    { length: SLOT_COUNT - visibleOrders.length },
    () => '<div class="order-slot empty-slot">Aguardando pedido</div>',
  );

  ordersGrid.innerHTML = [...visibleOrders.map(orderMarkup), ...emptySlots].join("");

  const waitingCount = Math.max(0, orders.length - SLOT_COUNT);
  panelStatus.textContent = waitingCount
    ? `${waitingCount} pedido${waitingCount === 1 ? "" : "s"} aguardando espaço`
    : "Todos os pedidos em preparo estão visíveis";
}

function playNotificationSound() {
  const AudioContext = window.AudioContext ?? window.webkitAudioContext;
  if (!AudioContext) return;

  const context = new AudioContext();
  const oscillator = context.createOscillator();
  const gain = context.createGain();
  oscillator.type = "sine";
  oscillator.frequency.setValueAtTime(880, context.currentTime);
  gain.gain.setValueAtTime(0.0001, context.currentTime);
  gain.gain.exponentialRampToValueAtTime(0.18, context.currentTime + 0.01);
  gain.gain.exponentialRampToValueAtTime(0.0001, context.currentTime + 0.22);
  oscillator.connect(gain);
  gain.connect(context.destination);
  oscillator.start();
  oscillator.stop(context.currentTime + 0.24);
  oscillator.addEventListener("ended", () => context.close());
}

function addOrder(order) {
  if (orders.some((currentOrder) => currentOrder.id === order.id)) return;
  orders.push(order);
  orders.sort((first, second) => first.createdAt.localeCompare(second.createdAt));
  renderOrders();
  playNotificationSound();
}

async function loadOrders() {
  try {
    const response = await fetch(`${API_BASE_URL}/orders`);
    if (!response.ok) throw new Error(`A API respondeu com status ${response.status}.`);
    orders = await response.json();
    orders.sort((first, second) => first.createdAt.localeCompare(second.createdAt));
    renderOrders();
  } catch (error) {
    renderOrders();
    panelStatus.textContent = "Não foi possível carregar os pedidos";
    showToast(error.message, "error");
  }
}

async function finishOrder(orderId, button) {
  button.disabled = true;
  button.textContent = "Finalizando...";

  try {
    const response = await fetch(`${API_BASE_URL}/orders/${orderId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ status: "Finalizado" }),
    });
    if (!response.ok) throw new Error(`A API respondeu com status ${response.status}.`);

    orders = orders.filter((order) => order.id !== orderId);
    selectedOrderId = null;
    renderOrders();
    showToast("Pedido finalizado");
  } catch (error) {
    selectedOrderId = null;
    renderOrders();
    showToast(`Não foi possível finalizar: ${error.message}`, "error");
  }
}

ordersGrid.addEventListener("click", (event) => {
  const slot = event.target.closest("[data-order-id]");
  if (!slot) return;

  const finishButton = event.target.closest("[data-finish-order]");
  if (finishButton) {
    finishOrder(slot.dataset.orderId, finishButton);
    return;
  }

  selectedOrderId =
    selectedOrderId === slot.dataset.orderId ? null : slot.dataset.orderId;
  renderOrders();
});

ordersGrid.addEventListener("keydown", (event) => {
  if (event.key !== "Enter" && event.key !== " ") return;
  const slot = event.target.closest("[data-order-id]");
  if (!slot) return;
  event.preventDefault();
  selectedOrderId =
    selectedOrderId === slot.dataset.orderId ? null : slot.dataset.orderId;
  renderOrders();
});

// Ponto de entrada reservado para o futuro consumer da mensageria.
window.kitchenPanel = { addOrder };

while (true) {
  const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
  await sleep(2000);
  loadOrders();
}
