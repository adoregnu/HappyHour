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

            var names = node.textContent.split(/[(),]/).filter(name => name.length > 1);
            actor['name'] = names[0].trim();
            if (names.length > 1) {
                actor['alias'] = names.slice(1);
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
        var name = _jav_parse_single_node("div[@class='actress-data']//dt[contains(.,'女優名')]/following-sibling::dd", null ,node);
        var names = name.split(/[（）()]/).filter(n => n.trim().length > 1);
        if (names.length < 1) {
            console.log('failed to parse actor name!');
            CefSharp.PostMessage({ type: 'items', data:0 });
            return;
        }
        var actor = {};
        var alias_array = [];

        actor['name'] = names[0].trim();
        if (names.length > 1) {
            names.slice(1).forEach(n => {
                tmp = n.trim();
                const check = /^[-–]/;
                if (check.test(tmp)) {
                    // discard english name, duplicated names are found!
                    //tmp = tmp.slice(1).trim().split(/[- ]/).reverse().join(' ')
                } else {
                    alias_array.push(tmp);
                }
            });
        }

        var img = _jav_parse_single_node("div[@class='actress-image']/img/@src", null, node);
        if (img != null) {
            actor['thumb'] = img;
        }
        var alias = _jav_parse_single_node("div[@class='actress-data']//dt[contains(.,'別名義')]/following-sibling::dd", null, node);
        if (alias != null) {
            var array = alias.split(/[（）、|・]/).filter(a => a.trim().length > 1);
            array.forEach(item => { 
                if (item.startsWith('– –')) { return; }
                if (item.startsWith('SOD')) { return; }
                if (!alias_array.includes(item.trim())) {
                    alias_array.push(item.trim());
                }
            });
        }
        if (alias_array.length > 0) {
            actor['alias'] = alias_array;
        }
        msg = {type: 'items', data:1, actor:[actor]};
        _post_message(msg, 'jp');
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
        maker: { xpath: "//dl[@class='dltable']/dt[text() = 'メーカー']/following-sibling::dd" },
        label: { xpath: "//dl[@class='dltable']/dt[contains(., 'レーベル')]/following-sibling::dd" },
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
    _post_message(msg, 'jp');
}) ();