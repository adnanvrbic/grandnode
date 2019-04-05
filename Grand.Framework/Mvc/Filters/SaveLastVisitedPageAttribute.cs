﻿using Grand.Core;
using Grand.Core.Data;
using Grand.Core.Domain.Customers;
using Grand.Core.Infrastructure;
using Grand.Services.Common;
using Grand.Services.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using System;

namespace Grand.Framework.Mvc.Filters
{
    /// <summary>
    /// Represents filter attribute that saves last visited page by customer
    /// </summary>
    public class SaveLastVisitedPageAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        public SaveLastVisitedPageAttribute() : base(typeof(SaveLastVisitedPageFilter))
        {
        }

        #region Nested filter

        /// <summary>
        /// Represents a filter that saves last visited page by customer
        /// </summary>
        private class SaveLastVisitedPageFilter : IActionFilter
        {
            #region Fields

            private readonly CustomerSettings _customerSettings;
            private readonly IGenericAttributeService _genericAttributeService;
            private readonly IWebHelper _webHelper;
            private readonly IWorkContext _workContext;
            private readonly ICustomerActivityService _customerActivityService;
            #endregion

            #region Ctor

            public SaveLastVisitedPageFilter(CustomerSettings customerSettings,
                IGenericAttributeService genericAttributeService,
                IWebHelper webHelper, 
                IWorkContext workContext,
                ICustomerActivityService customerActivityService)
            {
                this._customerSettings = customerSettings;
                this._genericAttributeService = genericAttributeService;
                this._webHelper = webHelper;
                this._workContext = workContext;
                this._customerActivityService = customerActivityService;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called before the action executes, after model binding is complete
            /// </summary>
            /// <param name="context">A context for action filters</param>
            public void OnActionExecuting(ActionExecutingContext context)
            {
                if (context == null || context.HttpContext == null || context.HttpContext.Request == null)
                    return;

                if (!DataSettingsHelper.DatabaseIsInstalled())
                    return;

                //only in GET requests
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                    return;

                //ajax request should not save
                bool isAjaxCall = context.HttpContext.Request.Headers["x-requested-with"] == "XMLHttpRequest";
                if (isAjaxCall)
                    return;

                //whether is need to store last visited page URL
                if (!_customerSettings.StoreLastVisitedPage)
                    return;

                //get current page
                var pageUrl = _webHelper.GetThisPageUrl(true);
                if (string.IsNullOrEmpty(pageUrl))
                    return;
                
                //get previous last page
                var previousPageUrl = _workContext.CurrentCustomer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.LastVisitedPage).GetAwaiter().GetResult();

                //save new one if don't match
                if (!pageUrl.Equals(previousPageUrl, StringComparison.OrdinalIgnoreCase))
                    _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, SystemCustomerAttributeNames.LastVisitedPage, pageUrl);

                if (!string.IsNullOrEmpty(context.HttpContext.Request.Headers[HeaderNames.Referer]))
                    if (!context.HttpContext.Request.Headers[HeaderNames.Referer].ToString().Contains(context.HttpContext.Request.Host.ToString()))
                    {
                        var previousUrlReferrer = _workContext.CurrentCustomer.GetAttribute<string>(_genericAttributeService, SystemCustomerAttributeNames.LastUrlReferrer);
                        var actualUrlReferrer = context.HttpContext.Request.Headers[HeaderNames.Referer];
                        if (previousUrlReferrer != actualUrlReferrer)
                        {
                            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, SystemCustomerAttributeNames.LastUrlReferrer, actualUrlReferrer);
                        }
                    }

                if (_customerSettings.SaveVisitedPage)
                {
                    if (!_workContext.CurrentCustomer.IsSearchEngineAccount())
                    {
                        _customerActivityService.InsertActivityAsync("PublicStore.Url", pageUrl, pageUrl, _workContext.CurrentCustomer.Id, _webHelper.GetCurrentIpAddress());
                    }
                }


            }

            /// <summary>
            /// Called after the action executes, before the action result
            /// </summary>
            /// <param name="context">A context for action filters</param>
            public void OnActionExecuted(ActionExecutedContext context)
            {
                //do nothing
            }

            #endregion
        }

        #endregion
    }
}