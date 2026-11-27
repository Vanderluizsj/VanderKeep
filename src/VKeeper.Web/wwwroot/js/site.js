// Progressive enhancement: prevent accidental duplicate submissions on valid POST forms
// while preserving browser back/forward cache (bfcache) usability.
document.addEventListener("submit", function (event) {
  if (event.defaultPrevented) {
    return;
  }

  const form = event.target;
  if (!(form instanceof HTMLFormElement)) {
    return;
  }

  if (form.method.toLowerCase() !== "post") {
    return;
  }

  if (form.dataset.vkeeperSubmitting === "true") {
    event.preventDefault();
    return;
  }

  if (window.jQuery && typeof window.jQuery(form).valid === "function" && !window.jQuery(form).valid()) {
    return;
  }

  form.dataset.vkeeperSubmitting = "true";

  const submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
  submitButtons.forEach(function (button) {
    if (!button.disabled) {
      button.dataset.vkeeperDisabled = "true";
      button.disabled = true;
    }
  });
});

window.addEventListener("pageshow", function () {
  document.querySelectorAll('form[data-vkeeper-submitting="true"]').forEach(function (form) {
    delete form.dataset.vkeeperSubmitting;
  });

  document.querySelectorAll('[data-vkeeper-disabled="true"]').forEach(function (button) {
    button.disabled = false;
    delete button.dataset.vkeeperDisabled;
  });
});
