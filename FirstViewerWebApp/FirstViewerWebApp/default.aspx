<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="FirstViewerWebApp.SamplePage" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Minimum ASP.NET Sample</title>
    <link type="text/css" rel="stylesheet" href="https://developer.api.autodesk.com/viewingservice/v1/viewers/style.css" />
</head>
<script src="https://autodeskviewer.com/viewers/2.2/viewer3D.min.js"></script>
<script>
    // This is the basic JavaScript sample code available at the documentation
    // It's optimized for 3D models

    // Show the model specified on the URN parameter
    function showModel(urn) {
        var options = {
            'document': 'urn:' + urn,
            'env': 'AutodeskProduction',
            'getAccessToken': getToken,
            'refreshToken': refreshToken,
        };
        var viewerElement = document.getElementById('viewer');
        var viewer = new Autodesk.Viewing.Viewer3D(viewerElement, {});
        Autodesk.Viewing.Initializer(
           options,
           function () {
               viewer.initialize();
               loadDocument(viewer, options.document);
           }
        );
    }

    // Load the document (urn) on the view object
    function loadDocument(viewer, documentId) {
        // Find the first 3d geometry and load that.
        Autodesk.Viewing.Document.load(
           documentId,
           function (doc) {// onLoadCallback
               var geometryItems = [];
               geometryItems = Autodesk.Viewing.Document.getSubItemsWithProperties(doc.getRootItem(), {
                   'type': 'geometry',
                   'role': '3d'
               }, true);
               if (geometryItems.length > 0) {
                   viewer.load(doc.getViewablePath(geometryItems[0]));
               }
           },
           function (errorMsg) {// onErrorCallback
               alert("Load Error: " + errorMsg);
           }
        );
    }

    // This calls are required if the models stays open for a long time and the token expires
    function getToken() {
        return makePOSTSyncRequest("auth.asmx/GetToken"); // get the current token in session
    }

    function refreshToken() {
        return makePOSTSyncRequest("auth.asmx/RefreshToken"); // force a new authentication
    }

    function makePOSTSyncRequest(url) {
        var xmlHttp = null;
        xmlHttp = new XMLHttpRequest();
        xmlHttp.open("POST", url, false);
        xmlHttp.send(null);
        return xmlHttp.responseText;
    }
</script>
<body>
    <form id="form1" runat="server">
        <div>This is a mininum sample in ASP.NET.<br />
            First edit the Credential.cs and enter your consumer key and consumer secret. Request at <a href="http://forge.autodesk.com">Forge portal</a>.<br />
            To use it you can either: (1) specify the URN or (2) upload a new file. Then click on "View Model" button</div>
        <hr />
        <div>
            Specify a odel URN:<asp:TextBox ID="modelURN" runat="server"></asp:TextBox>
            or upload a new model:
            <asp:FileUpload ID="fileToUpload" runat="server" /><br />
            <asp:Button ID="viewModel" runat="server" Text="View Model" OnClick="viewModel_Click" />
        </div>
        <hr />
        <div id="viewer" style="height: 600px; width: 1170px;">
        </div>
    </form>

</body>
</html>
