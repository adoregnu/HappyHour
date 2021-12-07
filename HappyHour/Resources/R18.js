(function () {
    const _PID = '{{pid}}';
    const INFO_CLASS_NAME = 'sc-cQDEqr jBzZAk'

    function get_node(node) { return node; }

    function _parseSingleNode(_xpath, _get_node = null) {
        var result = document.evaluate(_xpath, document.body,
            null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
        var node = result.singleNodeValue;
        if (node != null) {
            if (_get_node != null) {
                return _get_node(node);
            } else {
                return node.textContent.trim();
            }
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
        return array;
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
                //console.log('alias: ' + m[2]);
                var re = /([\w\s]+),?/ig;
                while ((arr = re.exec(m[2])) !== null) {
                    alias.push(arr[1].trim());
                }
                if (alias.length > 0) {
                    actorInfo['alias'] = alias;
                }
            }
            if (!node.src.endsWith('printing')) {
                actorInfo['thumb'] = node.src;
            }
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
            console.log('already parsing done!!');
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

    function parseSearchResult() {
        var result = _parseSingleNode('//*[@id="contents"]/div[2]/section/ul/li[1]/div/div');
        if (result == '0 titles found') {
            CefSharp.PostMessage({ type: 'items', data: 0 });
            return;
        }

        var pattern = 'id=(h_)?([0-9]+)?';
        if (_PID.includes('-')) {
            var tmp = _PID.split('-');
            pattern += tmp[0] + '0*' + tmp[1];
        } else {
            pattern += _PID;
        }
        const re = new RegExp(pattern, 'i');

        var url;
        var num_matched = 0;
        var nodes = _parseMultiNode("//li[starts-with(@class,'item-list')]/a");
        for (i = 0, node = nodes[i]; i < nodes.length; i++) {
            if (re.test(node.href)) {
                num_matched++;
                url = node.href;
            }
        }
        if (num_matched == 1) {
            console.log('redirect: ' + url);
            CefSharp.PostMessage({ type: 'url', data: url });
        }
        else if (num_matched > 1) {
            console.log('ambiguous, num_matched:' + num_matched);
        } else {
            console.log('no exact matching pid:');
            CefSharp.PostMessage({ type: 'items', data: 0 });
        }
    }

    function _checkServerError() {
        var err = document.querySelector('body > h1');
        if (err != null && err.textContent.includes('502 ERROR')) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
            return false;
        }
        return true;
    }

    var _is_scrolled = false;
    var _actor_exists = false;

    function _scroll() {
        var actor = _parseSingleNode("//span[contains(.,'Appearing in this movie')]", get_node);
        if (actor == null) {
            console.log("no actresses in this movie.");
            //window.scrollTo(0, document.body.scrollHeight / 2);
            _parsePage();
        } else {
            var rect = actor.parentElement.getBoundingClientRect();
            window.scrollTo(0, rect.top);
            _actor_exists = true;
        }
        _is_scrolled = true;
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
        }

        if (_is_scrolled == true) {
            if (_actor_exists && thumb_loaded) {
                _parsePage();
            }
        }
    }
    //console.log(window.location.href);

    if (!_checkServerError()) {
        return;
    }

    if (document.location.href.includes('/common/search/')) {
        parseSearchResult();
        return;
    }

    console.log("start MutationObserver");
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