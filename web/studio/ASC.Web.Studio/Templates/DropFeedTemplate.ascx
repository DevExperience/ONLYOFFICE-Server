﻿<%@ Control Language="C#" AutoEventWireup="false" EnableViewState="false" %>
<%@ Import Namespace="Resources" %>

<script id="dropFeedTmpl" type="text/x-jquery-tmpl">
    <div class="item">
        <div class="avatar">
            {{if byGuest}}
            <img src="${author.AvatarUrl}" title="${author.DisplayName}"/>
            {{else}}
            <a href="${author.ProfileUrl}" title="${author.DisplayName}" target="_blank"><img src="${author.AvatarUrl}"/></a>
            {{/if}}
        </div>
        <div class="content-box">
            <div class="description">
                <span class="menu-item-icon ${itemClass}" />
                <span class="product">${productText}.</span>
                {{if location}}
                <span class="location">${location}.</span>
                {{/if}}
                <span class="action">${actionText}</span>
                {{if groupedFeeds.length}}
                <span class="grouped-feeds-count">
                    ${ASC.Resources.Master.FeedResource.OtherFeedsCountMsg.format(groupedFeeds.length)}
                </span>
                {{/if}}
            </div>
            <div class="header">
                <a class="title" href="${itemUrl}" title="${title}" target="_blank">${title}</a>
            </div>
            <div class="date">
                {{if isToday}}
                <span><%= FeedResource.TodayAt + " " %>${displayCreatedTime}</span>
                {{else isYesterday}}
                <span><%= FeedResource.YesterdayAt + " " %>${displayCreatedTime}</span>
                {{else}}
                <span>${displayCreatedDatetime}</span>
                {{/if}}
            </div>
        </div>
    </div>
</script>