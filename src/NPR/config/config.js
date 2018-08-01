module.exports = {
    face : {
        key: process.env.FACE_API_KEY
    },
    function : {
        url : process.env.FUNCTION_URL || 'https://modnpr.azurewebsites.net/api/NPRFunction'
    }
  };