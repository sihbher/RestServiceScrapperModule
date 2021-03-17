# Rest Service Scrapper Module

This module takes and endpoint (or array of them) and scrap the endpoint every X seconds. You only need to stablish de env variables as follow



```json
 "RestModule": {
            "version": "1.0",
            "type": "docker",
            "status": "running",
            "restartPolicy": "always",
            "env": {
              "Endpoints": {
                "value": "http://192.168.0.1:5000/restserviceendpoint1,http://192.168.0.1:5000/restserviceendpoint2"
              },
              "CallFrequencySecs": {
                "value": "60"
              }
            },
            "settings": {
              "image": "${MODULES.RestServiceModule}",
              "createOptions": {
              }
            }
          }
```