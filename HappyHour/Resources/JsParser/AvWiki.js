(function () {
    const _PID = '{{pid}}';
    function get_node(n) { return n; }

    function _parse_actor(xpath) {
        var actorArray = _jav_parse_multi_node(xpath, get_node);
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

    function _parse_actor_page() {
        var node = _jav_parse_single_node("//div[contains(@class, 'actress-col')]", get_node);
        if (node == null) {
            console.log('no actress col');
            CefSharp.PostMessage({ type: 'items', data:0 });
            return;
        }
        actor = {};
        alias_array = [];
        var name = _jav_parse_single_node("div[@class='actress-data']//dt[contains(.,'女優名')]/following-sibling::dd", null ,node);
        names = name.split('-');
        //if (names.length > 1) { alias_array.push(names[1].trim()); }

        const regex = /(.+)(（.+）)?/;
        m = regex.exec(names[0]);
        if (m == null) {
            console.log('failed to parse actor name!');
            CefSharp.PostMessage({ type: 'items', data:0 });
            return;
        }
        names = m[1].split('（')
        actor['name'] = names[0];
        if (names.length > 1) {
            alias_array.push(names[1].slice(0,-1));
        }

        var img = _jav_parse_single_node("div[@class='actress-image']/img/@src", null, node);
        if (img != null) {
            actor['thumb'] = img;
        }
        var alias = _jav_parse_single_node("div[@class='actress-data']//dt[contains(.,'別名義')]/following-sibling::dd", null, node);
        if (alias != null) {
            var array = alias.split(/、|・/);
            array.forEach(function (item) {
                if (item.startsWith('– –')) { return; }
                m = regex.exec(item);
                if (m != null) {
                    names = m[1].split('（')
                    alias_array.push(names[0]);
                    if (names.length > 1) {
                        alias_array.push(names[1].slice(0, -1));
                    }
                }
            });
        }
        if (alias_array.length > 0) {
            actor['alias'] = alias_array;
        }
        console.log(JSON.stringify(actor));
        CefSharp.PostMessage({type: 'items', data:1, actor:[actor]});
    }

    function parseSearchResult() {
        var urls = _jav_parse_multi_node("//li[@class='search-readmore']/a/@href");
        if (urls == null) {
            urls = _jav_parse_multi_node("//div[@class='read-more']/a/@href");
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
        _parse_actor_page();
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
        studio: { xpath: "//dl[@class='dltable']/dt[contains(., 'メーカー')]/following-sibling::dd" },
        date: { xpath: "//dl[@class='dltable']/dt[contains(., '配信開始日')]/following-sibling::dd" },
        actor: {
            xpath: "//dl[@class='dltable']/dt[contains(., 'AV女優名')]/following-sibling::dd[1]/a",
            handler: _parse_actor
        },
    };

    var msg = { type : 'items' }
    var num_item = 0;
    for (var key in items) {
        var item = items[key];
        if (item["handler"] == null) {
            msg[key] = _jav_parse_single_node(item['xpath']);
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