var React = require("react");
var builders = require("./builders");
var Arg = require('./../../lib/arg');
var Postal = require('postal');

var labelStatus = {
	ok: 'label-default',
	error: 'label-warning',
	failed: 'label-danger',
	success: 'label-success'
}


module.exports = React.createClass({

	render: function(){
		if (this.props.active){
			return builders.toEditor(this.props);
		}

		var builder = builders.findEditor(this.props.cell.editor);
		var clazz = "cell label label-default";
		var text = builder.display(this.props.cell, this.props.value);
		if (!this.props.value){
			clazz = clazz + " missing";
			text = '[' + this.props.cell.key + ']';
		}
		



		var identifier = {step: this.props.id, cell: this.props.cell.key};

		var editCell = function(e){
			Postal.publish({
				channel: 'editor',
				topic: 'select-cell',
				data: identifier
			});

			e.preventDefault();

		}

		
		if (this.props.changed){
			clazz = clazz + " changed";
		}

		return (
			<span 
				onClick={editCell}
				onFocus={editCell}
				tabIndex="0" 
				role="button"
				data-cell={this.props.cell.key}
				className={clazz}
				title={this.props.cell.description || this.props.cell.key}>{text}</span>
		)
	}
});

