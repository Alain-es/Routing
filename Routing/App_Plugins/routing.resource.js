angular.module('umbraco.resources').factory('RoutingResource', function ($http) {
    return {

        getRoutes: function () {
            return $http.get("backoffice/Routing/RoutingApi/GetRoutes", {
                params: {}
            });
        },

        saveRoutes: function (configContent) {
            var data = JSON.stringify(angular.toJson(configContent));
            return $http.post("backoffice/Routing/RoutingApi/SaveRoutes", data);
        }

    };
})

