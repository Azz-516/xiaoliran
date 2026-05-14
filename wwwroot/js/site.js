// Toast notification
function showToast(msg, successOrOptions, durationOrOptions) {
    var toast = document.getElementById('appToast');
    if (!toast) return;
    toast.textContent = msg;
    toast.style.display = '';
    toast.className = 'app-toast show';

    var options = {};
    if (typeof successOrOptions === 'object') {
        options = successOrOptions;
    } else {
        options.success = successOrOptions !== false;
    }
    var duration = (typeof durationOrOptions === 'number') ? durationOrOptions : (typeof options === 'object' && typeof options.duration === 'number' ? options.duration : 3000);
    var success = options.success !== false;
    var type = options.type || (success ? 'success' : 'error');

    toast.classList.add('show');
    if (type === 'success') toast.classList.add('app-toast-success');
    else if (type === 'error') toast.classList.add('app-toast-error');
    else if (type === 'info') toast.classList.add('app-toast-info');
    else if (type === 'warning') toast.classList.add('app-toast-warning');

    setTimeout(function () {
        toast.className = 'app-toast';
        setTimeout(function () { toast.style.display = 'none'; }, 300);
    }, duration);
}

// Confirm dialog
var confirmCallback = null;
function showConfirmDialog(message, callback, title, confirmText) {
    var dialog = document.getElementById('confirmDialog');
    var msgEl = document.getElementById('confirmMessage');
    var titleEl = document.getElementById('confirmTitle');
    var okBtn = document.getElementById('confirmOk');
    if (!dialog || !msgEl) return;
    if (titleEl) titleEl.textContent = title || '确认删除';
    if (okBtn) okBtn.textContent = confirmText || '确认删除';
    msgEl.textContent = message;
    dialog.classList.add('show');
    confirmCallback = callback;
}

function closeConfirmDialog() {
    var dialog = document.getElementById('confirmDialog');
    if (dialog) {
        dialog.classList.remove('show');
    }
}

// Initialize confirm dialog buttons
document.addEventListener('DOMContentLoaded', function () {
    var confirmCancel = document.getElementById('confirmCancel');
    var confirmOk = document.getElementById('confirmOk');
    var confirmDialog = document.getElementById('confirmDialog');

    if (confirmCancel) {
        confirmCancel.addEventListener('click', function () {
            closeConfirmDialog();
        });
    }
    if (confirmOk) {
        confirmOk.addEventListener('click', function () {
            closeConfirmDialog();
            if (confirmCallback) confirmCallback();
            confirmCallback = null;
        });
    }
    if (confirmDialog) {
        confirmDialog.addEventListener('click', function (e) {
            if (e.target === this) {
                closeConfirmDialog();
            }
        });
    }
});
