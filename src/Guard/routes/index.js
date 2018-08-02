var express = require('express');
var router = express.Router();
var config = require("../config/config.js");
var storage = require('azure-storage');

/***************************************************************************
    Home page
***************************************************************************/
router.get('/', function (req, res, next) {

    var app;

    app = req.app;
    res.render('index', {
        config: config
    });
});

router.get('/poll', function (req, res, next) {
    // poll the queue, see if we have a message,
    var queueSvc = storage.createQueueService(config.queue.account, config.queue.accessKey);
    const QueueMessageEncoder = storage.QueueMessageEncoder;

    queueSvc.getMessages(config.queue.queueName, function (error, serverMessages) {
        if (error) {
            console.log(error);
            return;
        }

        var result = [];

        for (var i = 0; i < serverMessages.length; i++) {
            var encoder = new QueueMessageEncoder.TextBase64QueueMessageEncoder();

            var msg = JSON.parse(encoder.decode(serverMessages[i].messageText));

            result.push(msg);

            queueSvc.deleteMessage(config.queue.queueName, serverMessages[0].messageId, serverMessages[0].popReceipt, function (error) {
                if (!error) {
                    // Message deleted
                }
            });
        }

        res.send(result);
    });
});

module.exports = router;
