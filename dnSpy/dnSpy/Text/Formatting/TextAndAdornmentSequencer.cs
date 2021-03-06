﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Text.Formatting {
	sealed class TextAndAdornmentSequencer : ITextAndAdornmentSequencer {
		public IBufferGraph BufferGraph => textView.BufferGraph;
		public ITextBuffer SourceBuffer => textView.TextViewModel.EditBuffer;
		public ITextBuffer TopBuffer => textView.TextViewModel.VisualBuffer;

		readonly ITextView textView;
		readonly ITagAggregator<SpaceNegotiatingAdornmentTag> tagAggregator;

		public TextAndAdornmentSequencer(ITextView textView, ITagAggregator<SpaceNegotiatingAdornmentTag> tagAggregator) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (tagAggregator == null)
				throw new ArgumentNullException(nameof(tagAggregator));
			this.textView = textView;
			this.tagAggregator = tagAggregator;
			textView.Closed += TextView_Closed;
			tagAggregator.TagsChanged += TagAggregator_TagsChanged;
		}

		public event EventHandler<TextAndAdornmentSequenceChangedEventArgs> SequenceChanged;

		void TagAggregator_TagsChanged(object sender, TagsChangedEventArgs e) =>
			SequenceChanged?.Invoke(this, new TextAndAdornmentSequenceChangedEventArgs(e.Span));

		public ITextAndAdornmentCollection CreateTextAndAdornmentCollection(ITextSnapshotLine topLine, ITextSnapshot sourceTextSnapshot) {
			if (topLine == null)
				throw new ArgumentNullException(nameof(topLine));
			if (sourceTextSnapshot == null)
				throw new ArgumentNullException(nameof(sourceTextSnapshot));
			if (topLine.Snapshot.TextBuffer != TopBuffer)
				throw new InvalidOperationException();
			if (sourceTextSnapshot.TextBuffer != SourceBuffer)
				throw new InvalidOperationException();

			if (SourceBuffer != TopBuffer)
				throw new NotSupportedException();
			return CreateTextAndAdornmentCollection(topLine.ExtentIncludingLineBreak, sourceTextSnapshot);
		}

		public ITextAndAdornmentCollection CreateTextAndAdornmentCollection(SnapshotSpan topSpan, ITextSnapshot sourceTextSnapshot) {
			if (topSpan.Snapshot == null)
				throw new ArgumentException();
			if (sourceTextSnapshot == null)
				throw new ArgumentNullException(nameof(sourceTextSnapshot));
			if (topSpan.Snapshot.TextBuffer != TopBuffer)
				throw new InvalidOperationException();
			if (sourceTextSnapshot.TextBuffer != SourceBuffer)
				throw new InvalidOperationException();

			if (SourceBuffer != TopBuffer)
				throw new NotSupportedException();

			var list = new List<ISequenceElement>();
			var spaceTags = tagAggregator.GetTags(topSpan).ToArray();
			if (spaceTags.Length != 0)
				throw new NotImplementedException();//TODO: Use SpaceNegotiatingAdornmentTag, IAdornmentElement
			list.Add(new TextSequenceElement(BufferGraph.CreateMappingSpan(topSpan, SpanTrackingMode.EdgeExclusive)));
			Debug.Assert(list.Count == 1);// If it fails, make sure list is normalized
			return new TextAndAdornmentCollection(this, list);
		}

		void TextView_Closed(object sender, EventArgs e) {
			Debug.Assert(textView.Properties.ContainsProperty(typeof(ITextAndAdornmentSequencer)));
			textView.Properties.RemoveProperty(typeof(ITextAndAdornmentSequencer));
			textView.Closed -= TextView_Closed;
			tagAggregator.TagsChanged -= TagAggregator_TagsChanged;
			tagAggregator.Dispose();
		}
	}
}
