// v0.2.2.b — CleanTenant ManagementApp özel JS yardımcıları.
// Blazor JS interop ile çağrılır. Global window.cleantenant namespace'i altında toplanır.

window.cleantenant = window.cleantenant || {};

// ---------------------------------------------------------------------------
// OTP input bloğu: 6 ayrı kutucuk arasında otomatik focus + Backspace geri.
// Razor component OtpCodeInput.razor mount/unmount sırasında setup/teardown çağırır.
// ---------------------------------------------------------------------------
window.cleantenant.otpInput = (function () {
    // containerId → cleanup function map'i (unmount'ta listener'ları kaldırır)
    const wired = new Map();

    function setup(containerId) {
        teardown(containerId); // idempotent: zaten varsa eski listener'ları sök

        const container = document.getElementById(containerId);
        if (!container) {
            return;
        }
        const inputs = Array.from(container.querySelectorAll('input[data-otp-index]'));
        if (inputs.length === 0) {
            return;
        }

        const onInput = (e) => {
            const target = e.target;
            const idx = parseInt(target.dataset.otpIndex, 10);
            // Yalnız tek rakam tut; non-digit girişi sil
            const cleaned = (target.value || '').replace(/[^0-9]/g, '').slice(-1);
            target.value = cleaned;
            if (cleaned && idx < inputs.length - 1) {
                inputs[idx + 1].focus();
                inputs[idx + 1].select();
            }
        };

        const onKeyDown = (e) => {
            const target = e.target;
            const idx = parseInt(target.dataset.otpIndex, 10);
            if (e.key === 'Backspace') {
                if (!target.value && idx > 0) {
                    // Boş kutuda Backspace → önceki kutuyu sil ve oraya odaklan
                    e.preventDefault();
                    const prev = inputs[idx - 1];
                    prev.value = '';
                    prev.focus();
                    prev.select();
                    prev.dispatchEvent(new Event('input', { bubbles: true }));
                }
            } else if (e.key === 'ArrowLeft' && idx > 0) {
                e.preventDefault();
                inputs[idx - 1].focus();
                inputs[idx - 1].select();
            } else if (e.key === 'ArrowRight' && idx < inputs.length - 1) {
                e.preventDefault();
                inputs[idx + 1].focus();
                inputs[idx + 1].select();
            }
        };

        const onPaste = (e) => {
            e.preventDefault();
            const data = (e.clipboardData || window.clipboardData).getData('text') || '';
            const digits = data.replace(/[^0-9]/g, '').slice(0, inputs.length);
            for (let i = 0; i < inputs.length; i++) {
                inputs[i].value = digits[i] || '';
                inputs[i].dispatchEvent(new Event('input', { bubbles: true }));
            }
            const lastFilled = Math.min(digits.length, inputs.length) - 1;
            if (lastFilled >= 0) {
                inputs[Math.min(lastFilled + 1, inputs.length - 1)].focus();
            }
        };

        inputs.forEach(i => {
            i.addEventListener('input', onInput);
            i.addEventListener('keydown', onKeyDown);
            i.addEventListener('paste', onPaste);
        });

        // İlk kutuya odaklan
        if (inputs[0]) {
            try { inputs[0].focus(); } catch { /* sayfa yüklenme sırasında focus reddedilebilir */ }
        }

        wired.set(containerId, () => {
            inputs.forEach(i => {
                i.removeEventListener('input', onInput);
                i.removeEventListener('keydown', onKeyDown);
                i.removeEventListener('paste', onPaste);
            });
        });
    }

    function teardown(containerId) {
        const cleanup = wired.get(containerId);
        if (cleanup) {
            cleanup();
            wired.delete(containerId);
        }
    }

    return { setup, teardown };
})();

// ---------------------------------------------------------------------------
// OTP form (statik SSR Challenge sayfası için).
// Sayfa yüklenince çağrılır: 6 kutucuk OTP davranışı + form submit'inde
// kutucukların değerini hidden "code" input'una birleştirir + boş kutu varsa
// submit'i engeller. Recovery code modunda (input[data-otp-mode="text"])
// kutucuklar yerine düz textbox kullanılır; bu fonksiyon o durumda hiçbir şey
// yapmaz.
// ---------------------------------------------------------------------------
window.cleantenant.otpForm = (function () {
    function init(containerId, hiddenInputName, submitButtonId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        // Recovery mod'unda data-otp-mode="text" varsa görmezden gel.
        if (container.getAttribute('data-otp-mode') === 'text') return;

        // OTP input davranışını bağla (focus/backspace/paste).
        window.cleantenant.otpInput.setup(containerId);

        const inputs = Array.from(container.querySelectorAll('input[data-otp-index]'));
        const form = container.closest('form');
        const submitBtn = submitButtonId ? document.getElementById(submitButtonId) : null;

        const updateState = () => {
            const value = inputs.map(i => i.value || '').join('');
            // Hidden input'u güncel tut (form post'unda gönderilir)
            let hidden = form ? form.querySelector('input[name="' + hiddenInputName + '"][type="hidden"]') : null;
            if (!hidden && form) {
                hidden = document.createElement('input');
                hidden.type = 'hidden';
                hidden.name = hiddenInputName;
                form.appendChild(hidden);
            }
            if (hidden) hidden.value = value;
            if (submitBtn) {
                const ready = value.length === inputs.length;
                submitBtn.disabled = !ready;
                submitBtn.style.opacity = ready ? '1' : '0.5';
            }
        };

        inputs.forEach(i => i.addEventListener('input', updateState));
        // Form submit edilirken son bir kez güncelle (Enter ile submit dahil)
        if (form) form.addEventListener('submit', updateState);
        updateState();
    }

    return { init };
})();

// ---------------------------------------------------------------------------
// Browser'da blob olarak metin dosyası indirme (recovery code TXT export'u).
// Blazor InteractiveServer SignalR üzerinden çalıştığı için server'dan
// IJSRuntime ile çağrılır; file save dialog tarayıcı tarafında açılır.
// ---------------------------------------------------------------------------
window.cleantenant.downloadTextFile = function (filename, content) {
    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'download.txt';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 0);
};
