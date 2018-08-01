var express = require('express');
var router = express.Router();
var config = require("../config/config.js");

/***************************************************************************
    Home page
***************************************************************************/
router.get('/', function(req, res, next) {

    var app;

    app = req.app;
    res.render('index', {
        config: config
    });
});

module.exports = router;
