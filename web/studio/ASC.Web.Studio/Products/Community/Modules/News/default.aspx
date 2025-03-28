﻿<%@ Assembly Name="ASC.Web.Studio" %>
<%@ Assembly Name="ASC.Web.Community" %>

<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Products/Community/Modules/News/news.Master" EnableViewState="false" CodeBehind="default.aspx.cs" Inherits="ASC.Web.Community.News.Default" %>

<%@ Import Namespace="ASC.Web.Community.News.Resources" %>
<%@ Import Namespace="ASC.Web.Community.News.Code" %>
<%@ Import Namespace="ASC.Web.Community.News.Code.DAO" %>

<%@ Register Src="Controls/FeedItem.ascx" TagName="FeedItem" TagPrefix="fc" %>
<%@ Register Src="Controls/FeedView.ascx" TagName="FeedView" TagPrefix="fc" %>
<%@ Register TagPrefix="scl" Namespace="ASC.Web.Studio.UserControls.Common.Comments" Assembly="ASC.Web.Studio" %>
<%@ Register TagPrefix="sc" Namespace="ASC.Web.Studio.Controls.Common" Assembly="ASC.Web.Studio" %>

<asp:Content ID="PageContent" ContentPlaceHolderID="NewsContents" runat="server">
    <asp:Panel ID="MessageShow" runat="server" Visible="false">        
    </asp:Panel>
    <asp:Panel ID="ContentView" runat="server" CssClass="ContentView">
        <asp:Repeater ID="FeedRepeater" runat="server">
            <HeaderTemplate>
                <table class="tableBase" cellpadding="10" cellspacing="0">
                    <colgroup>
                        <col style="width: 80px;"/>
                        <col style="width: 80px;"/>
                        <col/>
                        <col style="width: 150px;"/>
                    </colgroup>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr class="news-row">
                    <fc:FeedItem ID="FeedControl" runat="server" Feed='<%#(Container.DataItem as Feed)%>'
                        FeedType='<%#Request["type"]%>' FeedLink='<%#string.Format("~/products/community/modules/news/?docid={0}{1}",(Container.DataItem as Feed).Id, Info.UserIdAttribute)%>'
                        IsEditVisible='<%#ASC.Web.Community.Product.CommunitySecurity.CheckPermissions(ASC.Web.Community.News.NewsConst.Action_Add)%>' EditUrlWithParam='<%#FeedItemUrlWithParam%>'>
                    </fc:FeedItem>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:Repeater>
        <% if(FeedsCount>0) { %>
        <div class="navigationLinkBox news">
            <table id="tableForNavigation" cellpadding="4" cellspacing="0">
                <tbody>
                <tr>
                    <td>
                        <div style="clear: right; display: inline-block;">
                            <sc:PageNavigator ID="pgNavigator" runat="server" EntryCount="1" />
                        </div>
                    </td>
                    <td style="text-align:right;">
                        <span class="gray-text"><%=NewsResource.Total%>: </span>
                        <span class="gray-text" style="margin-right: 20px;"><%=FeedsCount%></span>
                        <span class="gray-text"><%=NewsResource.ShowOnPage%>: </span>
                        <select class="top-align">
                            <option value="20">20</option>
                            <option value="50">50</option>
                            <option value="75">75</option>
                            <option value="100">100</option>
                        </select>
                    </td>
                </tr>
                </tbody>
            </table>
        </div>
        <% } %>
    </asp:Panel>
    <asp:Panel ID="FeedView" runat="server" Visible="false">
        <div class="eventsHeaderBlock header-with-menu" style="margin-bottom: 16px;">
            <span class="main-title-icon events"></span>
            <span class="header"><%=HttpUtility.HtmlEncode(EventTitle)%></span>
            <asp:Literal ID="SubscribeLinkBlock" runat="server"></asp:Literal>
            <span class="menu-small"></span>
        </div>
        <div id="viewItem">
            <fc:FeedView id="FeedViewCtrl" runat="server">
            </fc:FeedView>
            <scl:CommentsList ID="commentList" Simple="true" runat="server" Style="width: 100%;">
            </scl:CommentsList>
            <asp:HiddenField runat="server" ID="hdnField" />
        </div>
    </asp:Panel>
</asp:Content>
