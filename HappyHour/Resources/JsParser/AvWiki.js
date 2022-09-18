(function () {
    const _PID = '{{pid}}';

    function _parseSingleNode(_xpath, _getter = null, _node = document.body, _result = null, ) {
        var result = document.evaluate(_xpath, _node, null,
            XPathResult.FIRST_ORDERED_NODE_TYPE, _result);
        var node = result.singleNodeValue;
        if (node != null) {
            if (_getter != null) {
                return _getter(node);
            } else {
                return node.textContent.trim();
            }
        }
        return null;
    }
    function _parseMultiNode(xpath, _getter = null) {
        var result = document.evaluate(xpath, document.body,
            null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);

        var array = [];
        while (node = result.iterateNext()) {
            if (_getter != null) {
                array.push(_getter(node));
            } else {
                array.push(node.textContent.trim());
            }
        }
        if (array.length > 0) {
            return array;
        }
        return null;
    }
    function get_node(n) { return n; }

    function _parseActor(xpath) {
        var actorArray = _parseMultiNode(xpath, get_node);
        if (actorArray == null) {
            return null;
        }

        var array = [];
        for (var i = 0; i < actorArray.length; i++) {
            var node = actorArray[i];
            //console.log('href : ' + node.href);
            var actor = {};
            var m = /av-actress\/([a-z-]+)/i.exec(node.href);
            if (m != null) {
                if (m[1] == 'unknown') {
                    console.log('unknown actor!');
                    continue;
                }
                var tmp = m[1].replace('-', ' ');
                tmp = tmp.replace(/^([a-z])| ([a-z])/gi,
                    function (m) { return m.toUpperCase(); });
                actor['name'] = tmp.split(' ').reverse().join(' ');
                actor['alias'] = [node.textContent.trim()];
            } else {
                actor['name'] = node.textContent.trim();
            }
            actor['link'] = node.href;
            array.push(actor);
        }
        if (array.length > 0) {
            return array;
        }
        return null;
    }

    function _parseActorPage() {
        var node = _parseSingleNode("//div[contains(@class, 'actress-col')]", get_node);
        if (node == null) {
            console.log('no actress col');
            CefSharp.PostMessage({ type: 'items', data:0 });
            return;
        }
        actor = {};
        var name = _parseSingleNode("//dt[contains(.,'AV女優名')]/following-sibling::dd", null ,node);
        var m = /(.+)（.+）/.exec(name);
        //console.log(m[0]);
        actor['name'] = m[1];

        var img = _parseSingleNode("//div[@class='actress-image']/img/@src", null, node);
        if (img != null) {
            //console.log('thumb: ' + img);
            actor['thumb'] = img;
        }
        var alias = _parseSingleNode("//dt[contains(.,'別名義')]/following-sibling::dd", null, node);
        if (alias != null && alias != '' && !alias.includes('—') && alias.length < 3) {
            var alias_array = [];
            var array = alias.split(/、|・/);
            //console.log('alias:' + alias + ', count:' + array.length);
            array.forEach(function (item) {
                alias_array.push(item.split('（')[0]);
            });
            actor['alias'] = alias_array;
        }
        console.log(JSON.stringify(actor));
        CefSharp.PostMessage({type: 'items', data:1, actor:[actor]});
    }

    function parseSearchResult() {
        var urls = _parseMultiNode("//li[@class='search-readmore']/a/@href");
        if (urls == null) {
            urls = _parseMultiNode("//div[@class='read-more']/a/@href");
        }
        if (urls == null) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
            return;
        }
        if (urls.length == 1) {
            CefSharp.PostMessage({ type: 'url', data: urls[0] });
        } else {
            console.log('ambiguous result!');
        }
    }

    if (document.location.href.includes('/av-actress/')) {
        _parseActorPage();
        return;
    }

    if (document.location.href.includes('/?s=' + _PID)) {
        parseSearchResult();
        return;
    }

    var items = {
        title: { xpath : "//div[@class='article-header']/h1/text()"}, 
        cover: { xpath: "//div[contains(@class,'article-thumbnail')]/a/img/@src" },
        series: { xpath: "//dl[@class='dltable']/dt[contains(., 'シリーズ')]/following-sibling::dd" },
        studio: { xpath: "//dl[@class='dltable']/dt[contains(., 'レーベル')]/following-sibling::dd" },
        date: { xpath: "//dl[@class='dltable']/dt[contains(., '配信開始日')]/following-sibling::dd" },
        actor: {
            xpath: "//dl[@class='dltable']/dt[contains(., 'AV女優名')]/following-sibling::dd/a",
            handler: _parseActor
        },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            msg[key] = _parseSingleNode(item['xpath']);
        } else {
            msg[key] = item['handler'](item['xpath']);
        }
        if (msg[key] == null) {
            continue;
        }
        //console.log(key + ': ' + msg[key]);
        num_item += 1;
    }
    msg['data'] = num_item;
    console.log(JSON.stringify(msg));
    CefSharp.PostMessage(msg);
}) ();