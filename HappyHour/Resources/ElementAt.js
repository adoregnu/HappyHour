document.body.onmouseup = function () {
	//CefSharp.PostMessage can be used to communicate between the browser
	//and .Net, in this case we pass a simple string,
	//complex objects are supported, passing a reference to Javascript methods
	//is also supported.
	//See https://github.com/cefsharp/CefSharp/issues/2775#issuecomment-498454221 for details
	CefSharp.PostMessage(window.getSelection().toString());
}