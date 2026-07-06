window.trmMathJax = {
    isReady: false,

    typesetElement: async function (element) {
        if (typeof MathJax === 'undefined') {
            return;
        }
        if (!window.trmMathJax.isReady) {
            await MathJax.startup.promise;
            window.trmMathJax.isReady = true;
        }
        try {
            await MathJax.typesetPromise([element]);
        } catch (e) {
            console.warn('MathJax typeset failed:', e);
        }
    },

    typesetAll: async function () {
        if (typeof MathJax === 'undefined') {
            return;
        }
        if (!window.trmMathJax.isReady) {
            await MathJax.startup.promise;
            window.trmMathJax.isReady = true;
        }
        try {
            await MathJax.typesetPromise();
        } catch (e) {
            console.warn('MathJax typesetAll failed:', e);
        }
    }
};
