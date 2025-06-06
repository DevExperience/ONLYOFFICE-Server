﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="UserSubscriptions.ascx.cs" Inherits="ASC.Web.Studio.UserControls.Users.UserSubscriptions" %>


<script id="subscribtionObjectsTemplate" type="text/x-jquery-tmpl">
  {{if Objects.length > 0}}
    <div id="studio_subscriptions_${ItemId}_${SubItemId}_${TypeId}">
      {{each(i, obj) Objects}} 
		    <div id="studio_subscribeItem_${ItemId}_${SubItemId}_${TypeId}_${obj.Id}" class="clearFix">
		      <span class="sub-button">
		        <a class="on_off_button on" data-value="${obj.Id}" onclick="javascript:CommonSubscriptionManager.UnsubscribeObject('${ItemId}','${SubItemId}','${TypeId}',this);"></a>
		      </span>
		      <span>
            {{if obj.Url  == ''}}
              <a href="${obj.Url}" title="${obj.Name}">${obj.Name}</a>
            {{else}}
              ${obj.Name}
            {{/if}}
	        </span>
		    </div>
	    {{/each}}	   
    </div>
  {{/if}}
</script>

<script id="headerSubscriptionsTemplate" type="text/x-jquery-tmpl">
    {{each(i, item) Items}} 
       <span id="product_subscribeBox_${item.Id}" class="subs-module" onclick="CommonSubscriptionManager.ClickProductTag('${item.Id}')">
          {{if item.Class != ''}}
            <span class="subs-icon-module ${item.Class}"></span>			
          {{/if}}
          ${item.Name}          
        </span>
    {{/each}}
</script>
<script id="contentSubscriptionsTemplate" type="text/x-jquery-tmpl">
{{each(i, item) Items}} 
    <div id="content_product_subscribeBox_${item.Id}" class="tabs">
			
        <div id="studio_product_subscriptions_${item.Id}">
          {{if item.Type == 0}}
            <div id="studio_module_subscriptions_${item.Id}_${item.Id}" class="module-settings">	
              {{each(k, type) item.Types}} 		                                            
                <div id="studio_subscribeType_${item.Id}_${item.Id}_${type.Id}">
                  <div class="clearFix">
                    {{if !type.Single}}
                      <div class="type-name">
                        ${type.Name}
                      </div>	
                      <div id="studio_types_${item.Id}_${item.Id}_${type.Id}"></div>			        
                    {{else}}                      
                     <span class="sub-button">
                        {{if type.IsSubscribed}}
                          <a class="on_off_button on" href="javascript:CommonSubscriptionManager.UnsubscribeType('${item.Id}','${item.Id}','${type.Id}');" title="<%=Resources.Resource.UnsubscribeButton%>"></a>                                            
                        {{else}}
                          <a class="on_off_button off" href="javascript:CommonSubscriptionManager.SubscribeType('${item.Id}','${item.Id}','${type.Id}');" title="<%=Resources.Resource.SubscribeButton%>"></a>
                        {{/if}}                                            
                     </span>
                     <span>${type.Name}</span>
                    {{/if}}
                  </div>
                </div>
              {{/each}}
            </div>
          {{else}}
          <div class="subs-subtab">             
              {{if (item.Groups.length > 6)}}
                  {{if (item.Id == "1e044602-43b5-4d79-82f3-fd6208a11960")}}<span class="project-name"><%=Resources.Resource.SubscribtionProjectName %>: </span>{{/if}}
                  <select id="ListTabsCombobox_${item.Id}" class="comboBox">
                  {{each(j, group) item.Groups}}    
                  <option id="module_subscribeBox_${item.Id}_${group.Id}" data-id="${group.Id}" value="${j}" class="optionItem module">
                    ${group.Name}
                  </option>                      
                  {{/each}}  
                  </select>                     
              {{else}}
                {{each(j, group) item.Groups}}    
                  <span id="module_subscribeBox_${item.Id}_${group.Id}" data-id="${group.Id}" onclick="CommonSubscriptionManager.ClickModuleTag('${item.Id}','${group.Id}');" class="module">
                      ${group.Name}
                  </span> 
                 {{/each}}                            
               {{/if}}           
            </div>
            <div class="subs-subtab-contents">
             {{each(j, group) item.Groups}}
              <div id="content_module_subscribeBox_${item.Id}_${group.Id}" class="subtabs">
                {{each(k, type) group.Types}}
                  <div id="studio_subscribeType_${item.Id}_${group.Id}_${type.Id}" data-type="${type.Id}" class="subs-sections">
                    <div class="clearFix">
                      {{if !type.Single}}
                        <div class="type-name">
                          <b>${type.Name}</b>
                        </div>
                        <div id="studio_types_${item.Id}_${group.Id}_${type.Id}"></div>
                      {{else}}                         
                         <span class="sub-button">
                            {{if type.IsSubscribed}}
                              <a class="on_off_button on" href="javascript:CommonSubscriptionManager.UnsubscribeType('${item.Id}','${group.Id}','${type.Id}');" title="<%=Resources.Resource.UnsubscribeButton%>"></a>                                            
                            {{else}}
                              <a class="on_off_button off" href="javascript:CommonSubscriptionManager.SubscribeType('${item.Id}','${group.Id}','${type.Id}');" title="<%=Resources.Resource.SubscribeButton%>"></a>
                            {{/if}}
                         </span>
                         <span>${type.Name}</span>
                      {{/if}}
                     
                    </div>
                  </div>
                {{/each}}
              </div>
            {{/each}}
            </div>
          {{/if}}
       <div id="studio_product_subscribeBox_${item.Id}">

        <div class="tabs-block">
          <div class="subscribe-link">
            {{if item.CanUnSubscribe}}
              <a class="unsubscribe baseLinkAction" href="javascript:CommonSubscriptionManager.UnsubscribeProduct('${item.Id}', '${item.Name}');"><%=Resources.Resource.UnsubscribeButton%></a>
              <span> <%=Resources.Resource.SubscriptionFromAll%> ${item.Name}</span>
            {{/if}}
          </div>
            <span class="subs-notice-text"><%=Resources.Resource.SubscriptionNoticeVia%> </span>
            <select id="NotifyByCombobox_${item.Id}" class="comboBox notify-by-combobox display-none" onchange="CommonSubscriptionManager.SetNotifyByMethod('${item.Id}', jq(this).val());">
              {{if item.NotifyType == 0}}
                <option class="optionItem" value="0" selected="selected">
              {{else}}
                <option class="optionItem" value="0">
              {{/if}}
              <%=Resources.Resource.NotifyByEmail%></option>

              {{if item.NotifyType == 1}}
                <option class="optionItem" value="1" selected="selected">
              {{else}}
                <option class="optionItem" value="1">
              {{/if}}
              <%=Resources.Resource.NotifyByTMTalk%></option>

              {{if item.NotifyType == 2}}
                <option class="optionItem" value="2" selected="selected">
              {{else}}
                <option class="optionItem" value="2">
              {{/if}}
              <%=Resources.Resource.NotifyByEmailAndTMTalk%></option>
            </select>
        </div>
      </div>
        </div>	
      </div>   
  {{/each}}
</script>


<a name="subscriptions"></a>

<div class="subs-tabs" >
    <span id="product_subscribeBox_subNew" data-id="subNew" class="subs-module active" >
        <span class="subs-icon-module news"></span>
        <%=Resources.Resource.WhatsNewSubscriptionName%>
    </span>

<%if (IsAdmin()){%>
    
    <span id="product_subscribeBox_subAdmin" data-id="subAdmin" class="subs-module">        
        <span class="subs-icon-module admin"></span>
        <%=Resources.Resource.Administrator%>                
    </span>
 <%}%>   
     <div id="modules_notifySenders"></div>
     <div id="productSelector" class="subs-selector"></div>
</div>  


<div class="subs-contents">   
    <div id="content_product_subscribeBox_subNew" class="tabs active">
        <div>
            <div class="subs-only-button">
                <span id="studio_newSubscriptionButton" class="sub-button"><%=RenderWhatsNewSubscriptionState()%></span>
                <%= Resources.Resource.SubscribtionDailyNews%>
            </div>

            <%--What's new Notify by Combobox--%>
            <div>
                <span class="subs-notice-text"><%=Resources.Resource.SubscriptionNoticeVia%> </span>
                <%=RenderWhatsNewNotifyByCombobox() %>
            </div>
      </div>
    </div>
    
 <%if (IsAdmin()){%> 
    <div id="content_product_subscribeBox_subAdmin" class="tabs">
     <div>
        <div class="subs-only-button">
             <span id="studio_adminSubscriptionButton" class="sub-button"><%=RenderAdminNotifySubscriptionState()%></span>
             <%= Resources.Resource.SubscribtionAdminNotifications%>
        </div>

        <%--Admin Notify Notify By Combobox--%>
        <div>
            <span class="subs-notice-text"><%=Resources.Resource.SubscriptionNoticeVia%> </span>
            <%=RenderAdminNotifyNotifyByCombobox()%></div>
       </div>
     </div>
<%}%>    
    <div id="contents_notifySenders"></div>
</div>