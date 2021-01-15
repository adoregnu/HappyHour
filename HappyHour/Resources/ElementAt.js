/*
function createXPathFromElement(elm) {
    var allNodes = document.getElementsByTagName('*');
    for (var segs = []; elm && elm.nodeType == 1; elm = elm.parentNode) {
        if (elm.hasAttribute('id')) {
            var uniqueIdCount = 0;
            for (var n = 0; n < allNodes.length; n++) {
                if (allNodes[n].hasAttribute('id') && allNodes[n].id == elm.id) uniqueIdCount++;
                if (uniqueIdCount > 1) break;
            };
            if (uniqueIdCount == 1) {
                segs.unshift('id("' + elm.getAttribute('id') + '")');
                return segs.join('/');
            } else {
                segs.unshift(elm.localName.toLowerCase() + '[@id="' + elm.getAttribute('id') + '"]');
            }
        } else if (elm.hasAttribute('class')) {
            segs.unshift(elm.localName.toLowerCase() + '[@class="' + elm.getAttribute('class') + '"]');
        } else {
            for (i = 1, sib = elm.previousSibling; sib; sib = sib.previousSibling) {
                if (sib.localName == elm.localName) i++;
            };
            segs.unshift(elm.localName.toLowerCase() + '[' + i + ']');
        };
    };
    return segs.length ? '/' + segs.join('/') : null;
};
function lookupElementByXPath(path) {
    var evaluator = new XPathEvaluator();
    var result = evaluator.evaluate(path, document.documentElement, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    return result.singleNodeValue;
} */

/*
document.body.onmouseup = function (e) {
	//CefSharp.PostMessage can be used to communicate between the browser
	//and .Net, in this case we pass a simple string,
	//complex objects are supported, passing a reference to Javascript methods
	//is also supported.
	//See https://github.com/cefsharp/CefSharp/issues/2775#issuecomment-498454221 for details
	//console.log(e.clientX, e.clientY);
	elm = document.elementFromPoint(e.clientX, e.clientY);
	//var serializer = new XMLSerializer();
    console.log(elm.textContent)
	//console.log(serializer.serializeToString(elm));
    //console.log(createXPathFromElement(elm));
	CefSharp.PostMessage(window.getSelection().toString());
}
*/

var __prev;

document.body.onmouseover = function (event) {
    if (event.target === document.body ||
        (__prev && __prev === event.target)) {
        return;
    }
    if (__prev) {
        //__prev.className = __prev.className.replace(/\bhighlight\b/, '');
        //__prev.css('border','none')
        __prev = undefined;
    }
    if (event.target) {
        __prev = event.target;
        //__prev.className += "highlight";
        console.log(__prev.textContent.trim())
        //__prev.css('border','1px solid black')
    }
}