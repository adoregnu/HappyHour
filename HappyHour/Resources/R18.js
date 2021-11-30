const _PID = '{{pid}}';
const INFO_CLASS_NAME = 'sc-cQDEqr jBzZAk'

function _parseSingleNode(_xpath) {
    var result = document.evaluate(_xpath, document.body,
        null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    var node = result.singleNodeValue;
    if (node != null) {
        return node.textContent.trim();
    }
    return null;
}

function _parseMultiNode(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = result.iterateNext()) {
        array.push(node.textContent.trim());
    }
    if (array.length > 0) {
        return array;
    }
    return null;
}

function _parseActorThumb(xpath) {
    var result = document.evaluate(xpath, document.body,
        null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

    var array = [];
    while (node = result.iterateNext()) {
        var actorInfo = {};
        //console.log('alt: ' + node.alt);
        var m = /([\w\s]+)(?:\s)\((.+)\)/i.exec(node.alt);
        if (m == null) {
            actorInfo['name'] = node.alt;
        } else {
            actorInfo['name'] = m[1];
            var arr;
            var alias = [];
            console.log('alias: ' + m[2]);
            var re = /([\w\s]+),?/ig;
            while ((arr = re.exec(m[2])) !== null) {
                alias.push(arr[1].trim());
            }
            if (alias.length > 0) {
                actorInfo['alias'] = alias;
            }
        }
        actorInfo['thumb'] = node.src;
        array.push(actorInfo);
    }
    if (array.length > 0)
        return array;
    return null;
}

function _parseDate(xpath) {
    var strdate = _parseSingleNode(xpath);
    if (strdate == null) {
        return null;
    }
    return strdate.replace(/([\w.]+) (\d+), (\d+)/i,
        function (m, p1, p2, p3, off, str) {
            return p1.substring(0, 3) + ' ' + p2 + ' ' + p3;
        });
}

var _parsing_done = false;

function _parsePage() {
    if (_parsing_done) {
        return;
    }
    _parsing_done = true;

    var items = {
        title: { xpath: "//meta[@property='og:title']/@content" },
        date: {
            xpath: "//h3[contains(.,'Release date')]/following-sibling::div//text()",
            handler: _parseDate
        },
        //runtime: { xpath: "//h3[contains(.,'Runtime')]/following-sibling::div/text()" },
        //director: { xpath: "//h3[contains(.,'Director')]/following-sibling::div/text()" },
        series: { xpath: "//h3[contains(.,'Series')]/following-sibling::div//a/text()" },
        studio: { xpath: "//h3[contains(.,'Studio')]/following-sibling::div/a/text()" },
        label: { xpath: "//h3[contains(.,'Label')]/following-sibling::div//a/text()" },
        genre: {
            xpath: "//h3[contains(.,'Categories')]/following-sibling::div/span//text()",
            handler: _parseMultiNode
        },
        plot: { xpath: "//h3[contains(.,'synosis')]/following-sibling::p/text()" },
        cover: { xpath: "//div[@class='sc-cTJmaU BaBOz']/img/@src" },
        cover2: { xpath: "//div[@class='sc-cTJmaU bAAnmR']/img/@src" },
        actor: {
            xpath: "//div[contains(@class,'actress-switching')]//img",
            handler: _parseActorThumb
        },
    };

    var msg = { type : 'items'}
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (key == 'cover2') key = 'cover';
        if (msg[key] != null) continue;

        //in case of empty studio but not empty label
        if (key == 'label' && msg['studio'] == null) {
            key = 'studio';
        }
    
        if (item["handler"] == null) {
            msg[key] = _parseSingleNode(item['xpath']);
        } else {
            msg[key] = item['handler'](item['xpath']);
        }
        if (msg[key] == null) {
            continue;
        }
        num_item += 1;
    }
    msg['data'] = num_item;
    console.log(JSON.stringify(msg));
    CefSharp.PostMessage(msg);
}

function _multiResult() {
    var pattern = '/id=(h_)?(\d+)?';
    if (_PID.includes('-')) {
        var tmp = _PID.split('-');
        pattern += tmp[0] + '\d*' + tmp[1];
    } else {
        pattern += _PID;
    }
    pattern += '/i';

    const re = new RegExp(pattern);
    var result = document.evaluate("//li[starts-with(@class,'item-list')]/a",
        document.body, null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);
    var num_result = 0;
    while (node = result.iterateNext()) {
        num_result += 1;
        if (re.test(node.href) !== null) {
            CefSharp.PostMessage({ type: 'url', data: node.href });
            return 'redirected';
        }
    }
    if (num_result > 0) {
        return 'ambiguous';
    }
    return 'notfound';
}

function _checkResult() {
    var result = _parseSingleNode('//*[@id="contents"]/div[2]/section/ul/li[1]/div/div');
    if (result == '0 titles found') {
        CefSharp.PostMessage({ type: 'items', data: 0 });
        return false;
    }
    return true;
}

var _div_loaded = 0;
var _is_scrolled = false;
var _actor_exists = false;

function _scroll() {
    _is_scrolled = true;
    window.scrollTo(0, document.body.scrollHeight / 2);
    var actor = _parseSingleNode("//span[contains(.,'Appearing in this movie')]");
    if (actor == null) {
        console.log("no actresses in this movie.");
    } else {
        _actor_exists = true;
    }
}

function mutationCallback(mutations) {
    var thumb_loaded = false;
    for (var i = 0; i < mutations.length; i++) {
        var mutation = mutations[i];
        //console.log(mutation.type);
        if (mutation.type != 'childList') {
            continue;
        }

        mutation.addedNodes.forEach(function (node) {
            if (node.nodeName == 'DIV') {
                //console.log(node.nodeName + ' class=' + node.className);
                _div_loaded += 1;
                if (node.className == INFO_CLASS_NAME) {
                    _scroll();
                }
            }
            if (node.nodeName != "IMG") {
                return;
            }

            var ppp = node.parentNode.parentNode.parentNode;
            if (ppp.className != null && ppp.className.includes('actress-switch')) {
                //console.log(ppp.className);
                thumb_loaded = true;
            }
        });
        //console.log('_div_loaded: ' + _div_loaded);
    }

    if (_is_scrolled) {
        if ((!_actor_exists && _div_loaded == 6) ||
            (_actor_exists && thumb_loaded)) {
            _parsePage();
        }
    }
}

(function () {

    if (_multiResult() != 'notfound') {
        return;
    }

    if (!_checkResult()) {
        return;
    }

    const config = { attributes: true, childList: true, subtree: true };
    var observer = new MutationObserver(mutationCallback);
    observer.observe(document.body, config);

    var info = document.getElementsByClassName(INFO_CLASS_NAME);
    if (info.length > 0) {
        _scroll();
    }
    // Later, you can stop observing
    //observer.disconnect();
}) ();