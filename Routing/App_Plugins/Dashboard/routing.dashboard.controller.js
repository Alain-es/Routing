'use strict';
(function () {

    //Main controller
    function RoutingDashboardController($rootScope, $scope, $timeout, assetsService, RoutingResource, notificationsService) {

        $scope.columns = {
            "columns": [
                { "title": "Url Segments", "alias": "UrlSegments", "type": "textbox", "props": {} },
                { "title": "Enabled", "alias": "Enabled", "type": "checkbox", "props": {} },
                { "title": "DocumentType Alias", "alias": "DocumentTypeAlias", "type": "textbox", "props": {} },
                { "title": "Property Alias", "alias": "PropertyAlias", "type": "textbox", "props": {} },
                { "title": "Template", "alias": "Template", "type": "textbox", "props": {} },
                { "title": "Force Template", "alias": "ForceTemplate", "type": "checkbox", "props": {} },
                { "title": "Fallback NodeId", "alias": "FallbackNodeId", "type": "textbox", "props": {} },
                { "title": "Description", "alias": "Description", "type": "textarea", "props": {} }
            ]
        };

        $scope.value = [];
        $scope.actionInProgress = false;
        var propertiesEditorswatchers = [];
        var rowObject = {};

        var resetProertiesEditors = function () {
            rowObject = {};
            $scope.propertiesOrder = [];
            // clean watchers before set again.
            for (var index = 0; index < propertiesEditorswatchers.length; ++index) {
                propertiesEditorswatchers[index]();
            }
            angular.forEach($scope.columns.columns, function (value, key) {
                // Default values
                switch (value.alias) {
                    case "Enabled":
                        rowObject[value.alias] = "true";
                        break;
                    case "ForceTemplate":
                        rowObject[value.alias] = "false";
                        break;
                    default:
                rowObject[value.alias] = "";
                        break;
                }
                $scope.propertiesOrder.push(value.alias);
                var columnKey = key;
                var editorProperyAlias = value.alias;
            });
        }

        // Instantiate the grid with empty properties editors
        resetProertiesEditors();

        // Load the css file with the grid's styles
        assetsService.loadCss("/App_Plugins/Routing/Dashboard/routing.dashboard.css");

        // Load routes
        RoutingResource.getRoutes().then(
            function (response) {
                if (response.data) {
                    var Routes = JSON.parse(JSON.parse(response.data)).Routes;
                    if (Routes) {
                        if (Routes.Route) {
                            Routes.Route = jQuery.makeArray(Routes.Route);
                        }
                        $scope.value = Routes.Route;
                    }
                    // Check for deleted columns
                    angular.forEach($scope.value, function (row, key) {
                        angular.forEach(row, function (value, alias) {
                            if ($scope.propertiesOrder.indexOf(alias) == -1) {
                                delete row[alias];
                            }
                        });
                    });
                }
            },
            function (error) {
                var errorMessage = "";
                if (error.data && error.data.Message) {
                    errorMessage = error.data.Message;
                }
                notificationsService.error("Error retrieving routes from the config file", errorMessage);
                console.log(errorMessage);
            }
        );

        // New route
        $scope.addRow = function () {
            $scope.value.push(angular.copy(rowObject));
            var newrowIndex = $scope.value.length - 1;
            var newRow = $scope.value[newrowIndex];

            angular.forEach($scope.columns.columns, function (value, key) {
                var columnKey = key;
                var editorProperyAlias = value.alias;
            });
        }

        // Remove route
        $scope.removeRow = function (index) {
            $scope.value.splice(index, 1);
        }

        // Sort grid
        $scope.sortableOptions = {
            axis: 'y',
            cursor: "move",
            handle: ".sortHandle",
            start: function (event, ui) {
                var curTH = ui.helper.closest("table").find("thead").find("tr");
                var itemTds = ui.item.children("td");
                curTH.find("th").each(function (ind, obj) {
                    itemTds.eq(ind).width($(obj).width());
                });
            },
            update: function (ev, ui) {
                $timeout(function () {
                    $scope.rtEditors = [];
                    angular.forEach($scope.columns.columns, function (value, key) {
                        var columnKey = key;
                        var editorProperyAlias = value.alias;
                    });
                }, 0);
            }
        };

        // Save routes
        $scope.save = function () {
            $scope.actionInProgress = true;
            RoutingResource.saveRoutes({ "Route": $scope.value }).then(
                    function (result) {
                        if (result.data && result.data != "" && result.data != '""') {
                            notificationsService.error("Error saving routes", result.data);
                        }
                        else {
                            notificationsService.success("Routes saved successfully", "");
                        }
                        $scope.actionInProgress = false;
                    },
                    function (error) {
                        var errorMessage = "";
                        if (error.data && error.data.Message) {
                            errorMessage = error.data.Message;
                        }
                        notificationsService.error("Error saving routes", errorMessage);
                        $scope.actionInProgress = false;
                    }
                );
            //};
        };

    };

    // Register the controller
    angular.module("umbraco").controller('Routing.DashboardController', RoutingDashboardController);

})();
