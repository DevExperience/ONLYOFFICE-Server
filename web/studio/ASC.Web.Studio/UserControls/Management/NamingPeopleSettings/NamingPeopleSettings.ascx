<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="NamingPeopleSettings.ascx.cs" Inherits="ASC.Web.Studio.UserControls.Management.NamingPeopleSettings" %>
<%@ Import Namespace="ASC.Web.Studio.Utility" %>
<%@ Import Namespace="Resources" %>

<div class="clearFix">
    <div id="studio_namingPeople" class="settings-block">
        <div class="header-base clearFix" id="namingPeopleTitle">
            <%= Resource.NamingPeopleSettings %>
        </div>

            <asp:PlaceHolder ID="content" runat="server"></asp:PlaceHolder>
            <div class="middle-button-container">
                <a id="saveNamingPeopleBtn" class="button blue" href="javascript:void(0);"><%= Resource.SaveButton %></a>
            </div>

    </div>
    <div class="settings-help-block">
        <p><%=String.Format(Resource.HelpAnswerTeamTemplate, "<br />", "<b>", "</b>")%></p>
        <a href="<%= CommonLinkUtility.GetHelpLink(true) + "gettingstarted/configuration.aspx#CustomizingPortal_block" %>" target="_blank"><%= Resource.LearnMore %></a>
    </div>
</div>