window.trmMathJax = {
    isReady: false,

    ensureReady: async function (maxWaitMs = 5000) {
        const startedAt = Date.now();

        while (Date.now() - startedAt < maxWaitMs) {
            const mathJax = window.MathJax;
            const startupPromise = mathJax?.startup?.promise;

            if (startupPromise && typeof mathJax.typesetPromise === 'function') {
                await startupPromise;
                window.trmMathJax.isReady = true;
                return true;
            }

            await new Promise(resolve => setTimeout(resolve, 50));
        }

        return false;
    },

    typesetElement: async function (element) {
        if (!window.trmMathJax.isReady) {
            const ready = await window.trmMathJax.ensureReady();
            if (!ready) {
                return;
            }
        }

        try {
            await window.MathJax.typesetPromise([element]);
        } catch (e) {
            console.warn('MathJax typeset failed:', e);
        }
    },

    typesetAll: async function () {
        if (!window.trmMathJax.isReady) {
            const ready = await window.trmMathJax.ensureReady();
            if (!ready) {
                return;
            }
        }

        try {
            await window.MathJax.typesetPromise();
        } catch (e) {
            console.warn('MathJax typesetAll failed:', e);
        }
    }
};
