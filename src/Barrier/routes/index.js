var express = require('express');
var router = express.Router();


/***************************************************************************
    Home page
***************************************************************************/
router.get('/', function(req, res, next) {

    var app;

    app = req.app;
    res.render('index', {
    });
});

module.exports = router;
