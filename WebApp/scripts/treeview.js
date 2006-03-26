if (!Array.prototype.indexOf) {
	Array.prototype.indexOf = function(item) {
		var pos = 0;
		while (pos<this.length) {
			if (this[pos] == item)
				return pos;
			pos++;
		}
		return -1;
	}
}

YAHOO.widget.Node.prototype.appendChild = function(node) {
	var check = this;
	while (check) {
		if (check == node) {
			throw "Cannot append a node to one of it's descendants.";
		}
		check = check.parent;
	}
	
	if (node.parent && node.parent.children.indexOf(node)>=0)
		node.parent.removeChild(node);
	
	if (this.hasChildren()) {
    var sib = this.children[this.children.length - 1];
    sib.nextSibling = node;
    node.previousSibling = sib;
	}
	
	this.tree.regNode(node);
  node.parent = this;
  node.depth = this.depth + 1;
  node.multiExpand = this.multiExpand;
	this.children[this.children.length] = node;
	this.childrenRendered = false;
	
	return node;
}

YAHOO.widget.Node.prototype.insertBefore = function(node, ref) {
	if (ref == null)
		return this.appendChild(node);

	var check = this;
	while (check) {
		if (check == node) {
			throw "Cannot append a node to one of it's descendants.";
		}
		check = check.parent;
	}

	if (node.parent.children.indexOf(node)>=0)
		node.parent.removeChild(node);

	var pos = this.children.indexOf(ref);
	if (pos<0)
		throw "Not a child of this node";

	node.nextSibling = ref;
	node.previousSibling = ref.previousSibling;
	ref.previousSibling = node;
	
	this.tree.regNode(node);
  node.parent = this;
  node.depth = this.depth + 1;
  node.multiExpand = this.multiExpand;
	this.children.splice(pos, 0, node);
	this.childrenRendered = false;
	
	return node;
}

YAHOO.widget.Node.prototype.removeChild = function(node) {
	var pos = this.children.indexOf(node);
	if (pos<0)
		throw "Not a child of this node";
		
	if (node.nextSibling)
		node.nextSibling.previousSibling = node.previousSibling;
	if (node.previousSibling)
		node.previousSibling.nextSibling = node.nextSibling;
	node.nextSibling = null;
	node.previousSibling = null;
	
	this.children.splice(pos, 1);
	this.childrenRendered = false;
	return node;
}

var BlueprintIT = {
	widget: {}
}

BlueprintIT.widget.StyledTextNode = function(oData, oParent, expanded) {
	if (oParent) {
		this.init(oData, oParent, expanded);
		this.setUpLabel(oData);
		this.setUpStyles(oData);
	}
}

BlueprintIT.widget.StyledTextNode.prototype = new YAHOO.widget.TextNode();

BlueprintIT.widget.StyledTextNode.prototype.iconClass = "";
BlueprintIT.widget.StyledTextNode.prototype.labelClass = "";

BlueprintIT.widget.StyledTextNode.prototype.setUpStyles = function(oData) {
	if (oData.labelClass) {
		this.labelStyle = ' ' + oData.labelClass;
	}

	if (oData.iconClass) {
		this.iconClass = ' ' + oData.iconClass;
	}

	if (oData.fullClass) {
		this.labelStyle = ' ' + oData.fullClass;
		this.iconClass += ' ' + oData.fullClass;
	}
}

BlueprintIT.widget.StyledTextNode.prototype.getStyle = function() {
	var style = YAHOO.widget.TextNode.prototype.getStyle.call(this);
	style += this.iconClass;
	return style;
}

BlueprintIT.widget.StyledTextNode.prototype.getHoverStyle = function() {
	var style = YAHOO.widget.TextNode.prototype.getHoverStyle.call(this);
	style += this.iconClass;
	return style;
}

BlueprintIT.widget.DraggableTreeNodeProxy = function(node, sGroup) {
	if (node) {
		this.node = node;
		this.init(node.labelElId, sGroup);
		this.initFrame();
	}
}

BlueprintIT.widget.DraggableTreeNodeProxy.prototype = new YAHOO.util.DDProxy();

BlueprintIT.widget.DraggableTreeNodeProxy.prototype.node = null;

BlueprintIT.widget.DraggableTreeNodeProxy.prototype.indicatorDiv = null;

BlueprintIT.widget.DraggableTreeNodeProxy.prototype.getInsertPosition = function(node, e) {
	var ypos = YAHOO.util.Event.getPageY(e);
	
	var pos = 0;
	var subnode = node.children[0];
	while (subnode) {
		var elregion = YAHOO.util.Dom.getRegion(subnode.getEl());
		if (ypos<elregion.top) {
			return { parent: node, position: pos };
		}
		var chregion = YAHOO.util.Dom.getRegion(subnode.getChildrenEl());
		if (ypos<chregion.top) {
			if (ypos>((chregion.top+elregion.top)/2)) {
				if (subnode.hasChildren()) {
					return { parent: subnode, position: 0 };
				}
				else {
					return { parent: node, position: pos+1 };
				}
			}
			else {
				return { parent: node, position: pos };
			}
		}
		if (ypos<chregion.bottom) {
			return this.getInsertPosition(subnode, e);
		}
		if (ypos<elregion.bottom) {
			if (ypos>((elregion.top+elregion.bottom)/2)) {
				if (subnode.hasChildren()) {
					return { parent: subnode, position: 0 };
				}
				else {
					return { parent: node, position: pos+1 };
				}
			}
			else {
				return { parent: node, position: pos };
			}
		}
		pos++;
		subnode = subnode.nextSibling;
	}
	return { parent: node, position: pos };
}

BlueprintIT.widget.DraggableTreeNodeProxy.prototype.onDragDrop = function(e, id) {
	if (id != this.node.tree.getRoot().getChildrenElId())
		return;
		
	this.node.tree.indicatorDiv.style.visibility = "hidden";

	var point = this.getInsertPosition(this.node.tree.getRoot(), e);

	var before = null;
	if (point.position < point.parent.children.length)
		before = point.parent.children[point.position];
	
	point.parent.insertBefore(this.node, before);
	
	this.node.tree.draw();
}

BlueprintIT.widget.DraggableTreeNodeProxy.prototype.onDragOver = function(e, id) {
	if (id != this.node.tree.getRoot().getChildrenElId())
		return;
		
	var point = this.getInsertPosition(this.node.tree.getRoot(), e);

	var x;
	var y;

	if (point.position < point.parent.children.length) {
		var node = point.parent.children[point.position];
		var region = YAHOO.util.Dom.getRegion(node.getEl());
		y = region.top;
		if (node.labelElId)
			region = YAHOO.util.Dom.getRegion(node.labelElId);
		x = region.left;
	}
	else {
		var node = point.parent.children[point.parent.children.length-1];
		var region = YAHOO.util.Dom.getRegion(node.getEl());
		y = region.bottom;
		if (node.labelElId)
			region = YAHOO.util.Dom.getRegion(node.labelElId);
		x = region.left;
	}
	
	var el = this.getEl();
	var s = this.node.tree.indicatorDiv.style;
	s.width = (parseInt(el.offsetWidth) - 4) + "px";
	s.visibility = "";
	YAHOO.util.Dom.setXY(this.node.tree.indicatorDiv, [x, y]);
}

BlueprintIT.widget.DraggableTreeNodeProxy.prototype.onDragOut = function(e, id) {
	this.node.tree.indicatorDiv.style.visibility = "hidden";
}

BlueprintIT.widget.DraggableTreeNodeProxy.prototype.endDrag = function(e) {
	this.node.tree.indicatorDiv.style.visibility = "hidden";
}

BlueprintIT.widget.DraggableTreeView = function(id, dd) {
	if (id) {
		this.init(id);
		this.dragDropManager = dd;
		this.createIndicator();
	}
}

BlueprintIT.widget.DraggableTreeView.prototype = new YAHOO.widget.TreeView();

BlueprintIT.widget.DraggableTreeView.prototype.dragDropManager = null;

BlueprintIT.widget.DraggableTreeView.prototype.setupDD = function(node) {
	if (this.dragDropManager.canDrag(node)) {
		new BlueprintIT.widget.DraggableTreeNodeProxy(node);
	}
	
	var pos = 0;
	for (pos = 0; pos<node.children.length; pos++)
		this.setupDD(node.children[pos]);
}

BlueprintIT.widget.DraggableTreeView.prototype.createIndicator = function() {
	if (!this.indicatorDiv) {
    this.indicatorDiv = document.createElement("div");
    var s = this.indicatorDiv.style;
    s.position = "absolute";
    s.visibility = "hidden";
    s.border = "2px solid black";
		s.height = "0px";
    s.zIndex = 999;

    document.body.appendChild(this.indicatorDiv);
	}
};

BlueprintIT.widget.DraggableTreeView.prototype.draw = function() {
	YAHOO.widget.TreeView.prototype.draw.call(this);
	if (this.dragDropManager) {
		this.setupDD(this.getRoot());
		new YAHOO.util.DDTarget(this.getRoot().getChildrenElId());
	}
}

BlueprintIT.widget.TreeViewLoader = function() {
}

BlueprintIT.widget.TreeViewLoader.prototype = {
}

BlueprintIT.widget.TreeViewLoader.prototype.loadNode = function(treenode, item) {
	var nodes = [];
	var node = item.firstChild;
	while (node) {
		if ((node.nodeType == 1) && (node.tagName.toLowerCase() != "ul") && (node.tagName.toLowerCase() != "ol")) {
			nodes.push(node);
		}
		if (node.nodeType == 3) {
			var text = node.nodeValue;
			if (text.search(/^\s*$/)==0) {
				text=" ";
				if (nodes.length==0) {
					text=null;
				}
			}
			if (text) {
				if ((nodes.length>0) && (typeof nodes[nodes.length-1] == "string")) {
					nodes[nodes.length-1] += text;
				}
				else {
					nodes.push(text);
				}
			}
		}
		node = node.nextSibling;
	}
	var newnode;
	if (nodes.length==1) {
		if (typeof nodes[0] == "string") {
			//alert("Creating text node");
			newnode = new YAHOO.widget.TextNode({ label: nodes[0] }, treenode, true);
		}
		else if (nodes[0].tagName.toLowerCase() == "a") {
			//alert("Creating link node");
			newnode = new YAHOO.widget.TextNode({ href: nodes[0].href, label: nodes[0].innerHTML }, treenode, true);
		}
		else {
			//alert("Creating html node");
			newnode = new YAHOO.widget.HTMLNode({ html: nodes[0].outerHTML }, treenode, true, true);
		}
	}
	else {
		var html = "";
		var i=0;
		for (i=0; i<nodes.length; i++) {
			if (typeof nodes[i] == "string") {
				html += nodes[i];
			}
			else {
				html += nodes[i].outerHTML;
			}
		}
		//alert("Creating html node");
		newnode = new YAHOO.widget.HTMLNode({ html: html }, treenode, true, true);
	}

	var node = item.firstChild;
	while (node) {
		if ((node.nodeType == 1) && ((node.tagName.toLowerCase() == "ul") || (node.tagName.toLowerCase() == "ol"))) {
			this.loadNodeContents(newnode, node);
		}
		node = node.nextSibling;
	}

	newnode.expanded = newnode.hasChildren();
}

BlueprintIT.widget.TreeViewLoader.prototype.loadNodeContents = function(treenode, list) {
	var node = list.firstChild;
	while (node) {
		if ((node.nodeType == 1) && (node.tagName.toLowerCase() == "li")) {
			this.loadNode(treenode, node);
		}
		node = node.nextSibling;
	}
}

BlueprintIT.widget.TreeViewLoader.prototype.loadFromList = function(treeid, listid) {
	var tree = new BlueprintIT.widget.TreeView(treeid);
	var list = document.getElementById(listid);
	if (list && (list.tagName.toLowerCase()=="ul" || list.tagName.toLowerCase()=="ol")) {
		this.loadNodeContents(tree.getRoot(), list);
	}
	return tree;
}
