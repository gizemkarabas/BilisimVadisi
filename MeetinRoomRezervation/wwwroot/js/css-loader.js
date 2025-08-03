// CSS Loading Optimization to prevent FOUC
(function () {
    'use strict';

    // Function to load CSS asynchronously
    function loadCSS(href, before, media, attributes) {
        var doc = window.document;
        var ss = doc.createElement("link");
        var ref;
        if (before) {
            ref = before;
        } else {
            var refs = (doc.body || doc.getElementsByTagName("head")[0]).childNodes;
            ref = refs[refs.length - 1];
        }

        var sheets = doc.styleSheets;
        if (attributes) {
            for (var attributeName in attributes) {
                if (attributes.hasOwnProperty(attributeName)) {
                    ss.setAttribute(attributeName, attributes[attributeName]);
                }
            }
        }
        ss.rel = "stylesheet";
        ss.href = href;
        ss.media = "only x";

        function ready(cb) {
            if (doc.body) {
                return cb();
            }
            setTimeout(function () {
                ready(cb);
            });
        }

        ready(function () {
            ref.parentNode.insertBefore(ss, (before ? ref : ref.nextSibling));
        });

        var onloadcssdefined = function (cb) {
            var resolvedHref = ss.href;
            var i = sheets.length;
            while (i--) {
                if (sheets[i].href === resolvedHref) {
                    return cb();
                }
            }
            setTimeout(function () {
                onloadcssdefined(cb);
            });
        };

        function loadCB() {
            if (ss.addEventListener) {
                ss.removeEventListener("load", loadCB);
            }
            ss.media = media || "all";
        }

        if (ss.addEventListener) {
            ss.addEventListener("load", loadCB);
        }
        ss.onloadcssdefined = onloadcssdefined;
        onloadcssdefined(loadCB);
        return ss;
    }

    // Preload critical CSS
    window.loadCSS = loadCSS;

    // Font loading optimization
    if ('fonts' in document) {
        Promise.all([
            document.fonts.load('400 16px Roboto'),
            document.fonts.load('600 16px Poppins')
        ]).then(function () {
            document.documentElement.classList.add('fonts-loaded');
        });
    }

    // Prevent invisible text during font swap
    document.documentElement.style.setProperty('font-display', 'swap');

})();
