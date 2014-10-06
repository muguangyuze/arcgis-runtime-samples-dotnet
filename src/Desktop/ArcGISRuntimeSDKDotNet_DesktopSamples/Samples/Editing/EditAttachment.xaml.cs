﻿using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Layers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ArcGISRuntimeSDKDotNet_DesktopSamples.Samples
{
    /// <summary>
    /// Demonstrates how to update feature attachments in feature layer.
    /// </summary>
    /// <title>Edit Attachment</title>
    /// <category>Editing</category>
    public partial class EditAttachment: UserControl
    {
        public EditAttachment()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Selects feature for editing and query its attachments.
        /// </summary>
        private async void MyMapView_MapViewTapped(object sender, MapViewInputEventArgs e)
        {
            var layer = MyMapView.Map.Layers["Incidents"] as FeatureLayer;
            var table = (ArcGISFeatureTable)layer.FeatureTable;
            layer.ClearSelection();
            SetAttachmentEditor();
            string message = null;
            try
            {
                // Performs hit test on layer to select feature.
                var features = await layer.HitTestAsync(MyMapView, e.Position);
                if (features == null || !features.Any())
                    return;
                var featureID = features.FirstOrDefault();
                layer.SelectFeatures(new long[] { featureID });
                await QueryAttachmentsAsync(featureID);
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (!string.IsNullOrWhiteSpace(message))
                MessageBox.Show(message);
        }
        
        /// <summary>
        /// Prepares attachment editor for editing.
        /// </summary>
        private void SetAttachmentEditor(long featureID = 0, IReadOnlyList<AttachmentInfoItem> attachments = null)
        {
            AddButton.IsEnabled = featureID != 0;
            AttachmentList.Tag = featureID;
            AttachmentList.ItemsSource = attachments;
            AttachmentList.IsEnabled = attachments != null && attachments.Count > 0;
        }

        /// <summary>
        /// Submits attachment edits back to server.
        /// </summary>
        private async Task SaveEditsAsync()
        {
            var layer = MyMapView.Map.Layers["Incidents"] as FeatureLayer;
            var table = (ArcGISFeatureTable)layer.FeatureTable;
            if (table.HasEdits)
            {
                if (table is ServiceFeatureTable)
                {
                    var serviceTable = (ServiceFeatureTable)table;
                    // Pushes attachment edits back to the server.
                    var result = await serviceTable.ApplyAttachmentEditsAsync();
                    if (result.UpdateResults == null || result.UpdateResults.Count < 1)
                        return;
                    var updateResult = result.UpdateResults[0];
                    if (updateResult.Error != null)
                        throw updateResult.Error;
                }
            }
        }

        /// <summary>
        /// Query attachments of specified feature.
        /// </summary>
        private async Task QueryAttachmentsAsync(long featureID)
        {
            var layer = MyMapView.Map.Layers["Incidents"] as FeatureLayer;
            var table = (ArcGISFeatureTable)layer.FeatureTable;
            var attachments = await table.QueryAttachmentsAsync(featureID);
            if (attachments != null)
                SetAttachmentEditor(featureID, attachments.Infos);
        }

        /// <summary>
        /// Prompts user to pick file from pictures folder.
        /// </summary>
        private FileInfo GetFile()
        {
            var dialog = new OpenFileDialog();
             dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            dialog.Multiselect = false;
            dialog.Filter = "Image Files|*.tif;*.jpg;*.gif;*.png;*.bmp";
            var dialogResult = dialog.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
                return new FileInfo(dialog.FileName);
            return null;
        }

        /// <summary>
        /// Adds new attachment to feature.
        /// </summary>
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var featureID = (Int64)AttachmentList.Tag;
            var layer = MyMapView.Map.Layers["Incidents"] as FeatureLayer;
            var table = (ArcGISFeatureTable)layer.FeatureTable;
            var file = GetFile();
            if (file == null) return;
            string message = null;
            try
            {
                AttachmentResult addResult = null;
                using (var stream = file.OpenRead())
                {
                    addResult = await table.AddAttachmentAsync(featureID, stream, file.Name);
                }
                if (addResult != null)
                {
                    if (addResult.Error != null)
                        message = string.Format("Add attachment to feature [{0}] failed.\n {1}", featureID, addResult.Error.Message);
                    await SaveEditsAsync();
                    await QueryAttachmentsAsync(featureID);
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (!string.IsNullOrWhiteSpace(message))
                MessageBox.Show(message);
        }

        /// <summary>
        /// Opens the specified attachment.
        /// </summary>
        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            var info = (AttachmentInfoItem)((FrameworkElement)sender).DataContext;
            string message = null;
            try
            {
                using (var data = await info.GetDataAsync())
                {
                    if (data != Stream.Null)
                    {
                        var source = new BitmapImage();
                        source.BeginInit();
                        source.StreamSource = data;
                        source.EndInit();
                        var window = new Window();
                        window.Content = new Image() { Source = source };
                        window.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
        }

        /// <summary>
        /// Updates the specified attachment of feature.
        /// </summary>
        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var featureID = (Int64)AttachmentList.Tag;
            var info = (AttachmentInfoItem)((FrameworkElement)sender).DataContext;
            var layer = MyMapView.Map.Layers["Incidents"] as FeatureLayer;
            var table = (ArcGISFeatureTable)layer.FeatureTable;
            var file = GetFile();
            if (file == null) return;
            string message = null;
            try
            {
                AttachmentResult updateResult = null;
                using (var stream = file.OpenRead())
                {
                    updateResult = await table.UpdateAttachmentAsync(featureID, info.ID, stream, file.Name);
                }
                if (updateResult != null)
                {
                    if (updateResult.Error != null)
                        message = string.Format("Update on attachment [{0}] of feature [{1}] failed.\n {2}",info.ID, featureID, updateResult.Error.Message);
                    await SaveEditsAsync(); 
                    await QueryAttachmentsAsync(featureID);
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (!string.IsNullOrWhiteSpace(message))
                MessageBox.Show(message);
        }

        /// <summary>
        /// Deletes the specified attachment from feature.
        /// </summary>
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var featureID = (Int64)AttachmentList.Tag;
            var info = (AttachmentInfoItem)((FrameworkElement)sender).DataContext;
            var layer = MyMapView.Map.Layers["Incidents"] as FeatureLayer;
            var table = (ArcGISFeatureTable)layer.FeatureTable;
            string message = null;
            try
            {
                DeleteAttachmentResult deleteResult = null;
                    deleteResult = await table.DeleteAttachmentsAsync(featureID, new long[] { info.ID });
                if (deleteResult != null && deleteResult.Results != null && deleteResult.Results.Count > 0)
                {
                    var result = deleteResult.Results[0];
                    if (result.Error != null)
                        message = string.Format("Delete attachment [{0}] of feature [{1}] failed.\n {2}", info.ID, featureID, result.Error.Message);
                    await SaveEditsAsync(); 
                    await QueryAttachmentsAsync(featureID);
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }
            if (!string.IsNullOrWhiteSpace(message))
                MessageBox.Show(message);
        }
    }
}
