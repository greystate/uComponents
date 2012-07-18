﻿using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using uComponents.DataTypes.Shared.Extensions;
using uComponents.DataTypes.Shared.PrevalueEditors;
using umbraco;
using umbraco.cms.businesslogic.datatype;
using umbraco.editorControls;

namespace uComponents.DataTypes.SubTabs
{
    /// <summary>
    /// Control rendered in the cms when configuring the datatype.
    /// </summary>
    public class SubTabsPreValueEditor : AbstractJsonPrevalueEditor
    {
        private CheckBoxList tabsCheckBoxList = new CheckBoxList();

        private DropDownList typeDropDownList = new DropDownList();

        private CheckBox showLabelCheckBox = new CheckBox();

        private SubTabsOptions options = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubTabsPreValueEditor"/> class.
        /// </summary>
        /// <param name="dataType">Type of the data.</param>
        public SubTabsPreValueEditor(umbraco.cms.businesslogic.datatype.BaseDataType dataType)
            : base(dataType, umbraco.cms.businesslogic.datatype.DBTypes.Ntext)
        {
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        internal SubTabsOptions Options
        {
            get
            {
                if (this.options == null)
                {
                    this.options = this.GetPreValueOptions<SubTabsOptions>();

                    if (this.options == null)
                    {
                        this.options = new SubTabsOptions();
                    }
                }

                return this.options;
            }
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.tabsCheckBoxList.ID = "tabsCheckBoxList";
            this.tabsCheckBoxList.DataSource = uQuery.SqlHelper.ExecuteReader(@"

                SELECT id, text 
                FROM cmsTab
                ORDER BY text ASC

            ");

            this.tabsCheckBoxList.DataTextField = "text";
            this.tabsCheckBoxList.DataValueField = "id";
            this.tabsCheckBoxList.DataBind();
            this.tabsCheckBoxList.RepeatColumns = 5;
            this.tabsCheckBoxList.RepeatDirection = RepeatDirection.Horizontal;

            this.showLabelCheckBox.ID = "showLabelCheckBox";

            this.typeDropDownList.ID = "typeDropDownList";
            this.typeDropDownList.DataSource = Enum.GetNames(typeof(SubTabType));
            this.typeDropDownList.DataBind();


            this.Controls.Add(this.tabsCheckBoxList);
            this.Controls.Add(this.typeDropDownList);
            this.Controls.Add(this.showLabelCheckBox);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load"/> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!this.Page.IsPostBack)
            {
                ListItem checkBoxListItem;

                foreach (int tabId in this.Options.TabIds)
                {
                    checkBoxListItem = this.tabsCheckBoxList.Items.FindByValue(tabId.ToString());
                    if (checkBoxListItem != null)
                    {
                        checkBoxListItem.Selected = true;
                    }
                }
                
                this.typeDropDownList.SetSelectedValue(this.Options.SubTabType.ToString());
                this.showLabelCheckBox.Checked = this.Options.ShowLabel;
            }
        }

        /// <summary>
        /// Saves the options
        /// </summary>
        public override void Save()
        {
            if (this.Page.IsValid)
            {
                this.Options.TabIds.Clear();

                foreach (ListItem checkBoxListItem in this.tabsCheckBoxList.Items)
                {
                    if (checkBoxListItem.Selected)
                    {
                        this.Options.TabIds.Add(int.Parse(checkBoxListItem.Value));
                    }
                }

                // get the sub tab type from the drop down
                this.Options.SubTabType = (SubTabType)Enum.Parse(typeof(SubTabType), this.typeDropDownList.SelectedValue);

                this.Options.ShowLabel = this.showLabelCheckBox.Checked;

                this.SaveAsJson(this.Options);
            }
        }

        /// <summary>
        /// Replaces the base class writer and instead uses the shared uComponents extension method, to inject consistant markup
        /// </summary>
        /// <param name="writer"></param>
        protected override void RenderContents(HtmlTextWriter writer)
        {
            writer.AddPrevalueRow("Tabs", this.tabsCheckBoxList);
            writer.AddPrevalueRow("Type", this.typeDropDownList);
            writer.AddPrevalueRow("Show Label", this.showLabelCheckBox);
        }
    }
}
