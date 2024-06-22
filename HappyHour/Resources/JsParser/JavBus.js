(function () {
    const _PID = '{{pid}}';
    function parseSearchResult() {
        var urls = _jav_parse_multi_node("//a[@class='movie-box']/@href");
        if (urls == null) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
        } else if (urls.length == 1) {
            CefSharp.PostMessage({ type: 'url', data: urls[0] });
        } else {
            console.log('ambiguous result!');
        }
    }

    function get_node(node) { return node; }

    function parseCover(xpath) {
        var img = _jav_parse_single_node(xpath, get_node);
        if (img != null) {
            return img.src;
        }
        return null;
    }

    function parseActor(xpath) {
        var result = _jav_parse_multi_node(xpath, get_node);
        if (result == null) {
            console.log("no actors");
        } else {
            console.log(result.length + " actors");
        }
    }

    if (document.location.href.includes('/search/')) {
        parseSearchResult();
        return;
    }

    var items = {
        title: { xpath: "//div[@class='container']/h3/text()" },
        cover: { xpath: "//a[@class='bigImage']/img", handler: parseCover },
        date: { xpath: "//span[contains(.,'Release Date:')]/following-sibling::text()"},
        studio: { xpath: "//span[contains(.,'Studio:')]/following-sibling::a/text()"},
        series: { xpath: "//span[contains(.,'Series:')]/following-sibling::a/text()" },
        genre: { xpath: "//p[contains(.,'Genre:')]/following-sibling::p/span//a/text()", handler: _jav_parse_multi_node },
        actor: { xpath: "//p[@class='star-show']/following-sibling::p//a", handler: parseActor }
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