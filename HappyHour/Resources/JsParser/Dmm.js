(function () {
    const _PID = '{{pid}}';
    function get_node(node) { return node; }
    function _parse_search_result() {
        var nodes = _jav_parse_multi_node("//div[@class='flex py-1.5 pl-3']/a", get_node);
        //var nodes = _jav_parse_multi_node("//div[@class='mx-3 mt-1.5 mb-3 h-40']", get_node);
        if (nodes== null) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
            return;
        }

        var filters = ['/dvd/', '/content/']
        // replace last occurrence of '-' in _PID with ''
        var pid = _PID.replace(/-([^ -]*)$/, '$1').toLowerCase();

        // filter nodes by filters
        nodes = nodes.filter(n => {
            var href = n.href.toLowerCase();
            if (!href.includes(pid)) return false;
            return filters.some(f => href.includes(f));
        });
        if (nodes.length == 0) {
            CefSharp.PostMessage({ type: 'items', data: 0 });
            return;
        }

        // sort node by href
        /*
            https://www.dmm.co.jp/mono/dvd/-/detail/=/cid=hsoda100/?i3_ref=search&i3_ord=2 
            https://video.dmm.co.jp/av/content/?id=hsoda00100&i3_ref=search&i3_ord=1
        */
        nodes.sort((a, b) => {
            var ahref = a.href.split('/')[4];
            var bhref = b.href.split('/')[4];
            if (ahref < bhref) return 1;
            if (ahref > bhref) return -1;
            return 0;
        });
        nodes[0].click();
    }

    function _parse_actor(xpath) {
        var node = _jav_parse_single_node(xpath, get_node);
        if (node == null) {
            console.log('no actor node');
            return null;
        }
        var img = _jav_parse_single_node("a/span[@class='img']/img/@src", null, node);
        var name = _jav_parse_single_node("a/span[@class='ttl']", null, node);
        var array = name.split(/[（）()]/).filter(n => n.trim().length > 1);
        var alias = [];
        if (array.length > 1) { 
            name = array[0].trim();
            for (var i = 1; i < array.length; i++) {
                alias.push(array[i].trim());
            }
        }
        return [{ name: name, thumb: img, alias: alias }];
    }

    function _multi_actor(xpath) {
        var names = _jav_parse_multi_node(xpath);
        if (names == null) {
            return null;
        }
        //木下ひまり（花沢ひまり）
        return names.map(name => {
            var parts = name.split(/[（）()]/).filter(n => n.trim().length > 0);
            if (parts.length > 1) {
                return { name: parts[0], alias: parts[1] };
            } else {
                return { name: parts[0] };
            }
        });
    }

    function _polish_title(xpath) {
        var remove_patterns = [/^【.+】/, '（ブルーレイディスク）']
        return _polish_single_node(xpath, remove_patterns);
    }

    if (document.location.href.includes('/search/=')) {
        _parse_search_result();
        return;
    }
    var items = null;
    if (document.location.href.includes('/content/')) {
        items = {
            title: {
                xpath: "/html/body/div[3]/main/div[3]/div[2]/div/div[1]/h1/span",
                handler: _polish_title
            },
            cover: { xpath: "//*[@data-e2eid='sample-image-gallery']/div[2]/a/@href" },
            // 開始日 or 発売日
            date: { xpath: "//th[contains(.,'開始日：') or contains(.,'発売日：')]/following-sibling::td"},
            series: { xpath: "//th[contains(.,'シリーズ')]/following-sibling::td"},
            maker: { xpath: "//th[contains(.,'メーカー：')]/following-sibling::td"},
            label: { xpath: "//th[contains(.,'レーベル：')]/following-sibling::td" },
            genre: {
                xpath: "//th[contains(.,'ジャンル')]/following-sibling::td//a",
                handler: _jav_parse_multi_node
            },
            actor: { xpath: "//th[contains(.,'出演者')]/following-sibling::td//a", handler: _multi_actor },
            plot: { xpath: "/html/body/div[3]/main/div[3]/div[2]/div/div[2]/div[1]/div[2]/div/div" }
        };
    }
    else if (document.location.href.includes('/dvd/')) {
        items = {
            title: { xpath: "//*[@id='title']", handler: _polish_title },
            cover: { xpath: "//*[@property='og:image']/@content" },
            date: { xpath: "//*[@class='wrapper-product']//td[preceding-sibling::td[contains(.,'発売日')]]" },
            actor: { xpath: "//*[@class='wrapper-product']//td[preceding-sibling::td[contains(.,'出演者')]]//a" },
            series: { xpath: "//*[@class='wrapper-product']//td[preceding-sibling::td[contains(.,'シリーズ')]]/a" },
            maker: { xpath: "//*[@class='wrapper-product']//td[preceding-sibling::td[contains(.,'メーカー')]]/a" },
            label: { xpath: "//*[@class='wrapper-product']//td[preceding-sibling::td[contains(.,'レーベル')]]/a" },
            genre: {
                xpath: "//*[@class='wrapper-product']//td[preceding-sibling::td[contains(.,'ジャンル')]]/a",
                handler: _jav_parse_multi_node
            },
            plot: {
                xpath: "//*[@id='mu']/div/table/tbody/tr/td[1]/div[3]/p",
                handler: function (xpath) {
                    var remove_patterns = [/※こちらは.+/]
                    return _polish_single_node(xpath, remove_patterns);
                }
            },
            //actor: { xpath: "//*[@class='tmb-actress-large']", handler: _parse_actor }
            actor: { xpath: "//*[@id='performer']/a", handler: _multi_actor },
        };
    }
    // wait for page to load
    console.log('Page state: ' +  document.readyState);
    /*
    var waitForPageLoad = setInterval(function () {
        if (document.readyState === "complete") {
            clearInterval(waitForPageLoad);
        }
    }, 100);
    */

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