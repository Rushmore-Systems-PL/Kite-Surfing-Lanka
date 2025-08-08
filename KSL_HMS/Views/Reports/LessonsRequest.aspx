<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="LessonsRequest.aspx.cs" Inherits="KSL_HMS.Views.Reports.LessonsRequest" %>
<%@ Register Assembly="Microsoft.ReportViewer.WebForms" Namespace="Microsoft.Reporting.WebForms" TagPrefix="rsweb" %>

<form id="form1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
    <rsweb:ReportViewer ID="ReportViewer1" runat="server" Width="100%" AsyncRendering="false" SizeToReportContent="true">
    </rsweb:ReportViewer>
</form>
