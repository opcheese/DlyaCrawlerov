//mongodb username = crawler, pass = crawlerpasswd

var tabs = require('tabs');
var request = require("request").Request;
var timers = require("timers");
var notifications = require("notifications");
var ss = require("simple-storage");
var {Cc, Ci} = require("chrome");

var baseApiPath = 'https://api.mongolab.com/api/1';
var apiKey = '50711914e4b0a72f28359bd1';

var databases = null;
var collections = null;

var collectionsObtained = false;

if (!ss.storage.userID)
    ss.storage.userID = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, 
        function(c) {
            var r = Math.random()*16|0, v = c == 'x' ? r : (r&0x3|0x8);
        	return v.toString(16);
	    }
    );

tabs.on('ready', function(tab) {
    prepareToAndProcessTabWithTab(tab)
});

function prepareToAndProcessTabWithTab(tab) {
    logWithMsg('userID = ' + ss.storage.userID);
    logWithMsgAndParamsArray("page loaded", [tab.title, tab.url]);

    if (!collectionsObtained) {
		getDatabases();
	}
	processTabWithTab(tab);
}

function processTabWithTab(tab) {
	if (!collectionsObtained) {
    	timers.setTimeout(function() { processTabWithTab(tab) }, 3000);
    	return;
	}

	var tabContent = null;
	tab.attach({
		contentScript: 'self.postMessage(document.documentElement.innerHTML);',
		onMessage: function (message) {
			logWithMsgAndParamsArray("extracting content from", [tab.title, tab.url]);
			processContentWithTitleAndURL('<html>' + message + '</html>', tab.title, tab.url);
		}
	});
}

function processContentWithTitleAndURL(content, title, url) {
	// logWithMsg('content = ' + content);
	if (!content) {
		logWithMsg("page content is empty.");
		return;
	}

	var linkRegex = /<\s*a[^<>]*href\s*=\s*("|')([^\/"].+?)("|')/gim;
	var links = [];
	while (match = linkRegex.exec(content)) {
		links.push(match[2]);
	}

	logWithMsgAndParamsArray("links = ", links);

	var pageInfo = {
		'user': ss.storage.userID,
		'url': url,
		'title': title,
		'content': content,
		'links': links
	};

    logWithMsg(pageInfo);

	sendToDBWithCollectionAndDatabase(JSON.stringify(pageInfo), 'pages', 'crawler');

	// var parser = Cc["@mozilla.org/xmlextras/domparser;1"].createInstance(Ci.nsIDOMParser);
	// var doc = parser.parseFromString(content, 'text/xml');
	// logWithMsg(doc.documentNode);
	// var linkIterator = doc.evaluate('//a', doc, null, Ci.nsIDOMXPathResult.ORDERED_NODE_ITERATOR_TYPE, null);
	// try {
	// 	var thisNode = linkIterator.iterateNext();

	// 	while(thisNode) {
	// 		alert(thisNode.textContent);
	// 		thisNode = linkIterator.iterateNext();
	// 	}
	// }
	// catch (e) {
	// 	logWithMsg(e);
	// }
}

//mongoDB functions

function makeRequestUrlWithRequest(request) {
	var needToInsertSplash = request.indexOf('/') != 0;
	return baseApiPath + (needToInsertSplash ? '/' : '') + request + '?apiKey=' + apiKey;
}

function getDatabases() {
	logWithMsg('request databases');

	var requestUrl = makeRequestUrlWithRequest('databases');
	sendRequest(requestUrl, null, processGetDatabasesRequestResponse);
}

function processGetDatabasesRequestResponse(response) {
	databases = response.match(/\b[\w\d.-]+\b/g);

	logWithMsg(databases);

	if (databases.indexOf('crawler') > -1) {
    	getCollectionsWithDatabase('crawler');
    }
}

function getCollectionsWithDatabase(database) {
	logWithMsg('request collections in ' + database);

	var requestUrl = makeRequestUrlWithRequest('databases/' + database + '/collections');
	sendRequest(requestUrl, null, processGetCollectionsRequestResponse);
}

function processGetCollectionsRequestResponse(response) {
	collections = response.match(/\b[\w\d.-]+\b/g);

	logWithMsg(collections);
	collectionsObtained = true;
}

function sendToDBWithCollectionAndDatabase(data, collection, database) {
    logWithMsg('sending info to ' + collection + ' in ' + database);
	var requestUrl = makeRequestUrlWithRequest('databases/' + database + '/collections/' + collection);
	sendRequest(requestUrl, data, notifyWithResponse);
}

function notifyWithResponse(response) {
	notifyWithTitleAndText('Crawler', 'Page successfully sended.');
}

// request functions

function sendRequest(url, postData, callback) {
	var funcName = 'sendRequest(url = ' + url + ', postData = ' + postData + ', callback = ' + callback;
	var headers = null;
	if (postData) {
		headers = {'Content-type': 'application/json'};
	}

	newRequest = request({
		url: url,
		content: postData,
        headers: headers,
		onComplete: function (response) {
			if (response.status == '200' || response.status == '201') {
				callback(response.text);
			} else {
				logErrorWithMsg(response.status + ' — ' + response.statusText);
				collectionsObtained = false;
			}
		}
	});

	if (postData) {
		newRequest.post();
	} else {
		newRequest.get();
	}
}

// logging functions

function logWithMsg(msg) {
	console.log(msg);
}

function logWithMsgAndParamsArray(msg, params) {
    console.log(msg, ['[', params.join(', '), ']'].join(''));
}

function logErrorWithMsg(msg) {
	console.log('ERROR:', msg);
}

function logErrorWithFuncAndMsg(func, msg) {
	console.log('ERROR in ' + func + ':', msg);
}

function notifyWithTitleAndText(title, text) {
	notifications.notify({
		title: title,
		text: text
	});
}