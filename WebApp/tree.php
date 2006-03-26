<?

require 'logging.php';
require 'storage.php';

LoggerManager::setLogLevel('', SWIM_LOG_WARN);

$log = LoggerManager::getLogger('core');

$db = new Storage("");

function displayComponent($component, $system, $node)
{
	global $db;
	
	$result = $db->query('SELECT rowid,id FROM component WHERE parent='.$component.';');
	while ($row = $result->fetch())
	{
		$id = $row['rowid'];
		$name = $row['id'];
		print("\tvar comp".$id." = new BlueprintIT.widget.StyledTextNode({ label: '".$name."',  href: 'component.php?id=".$id."&system=".$system."', target: 'detail', iconClass: 'component component-".$name."' }, ".$node.", false);\n");
		//displayComponent($id, $system, "comp".$id);
	}

}

function displaySite($site, $node)
{
	global $db;
	
	$result = $db->query('SELECT rowid,name FROM site WHERE parent='.$site.';');
	while ($row = $result->fetch())
	{
		$id = $row['rowid'];
		$name = $row['name'];
		print("\tvar site".$id." = new BlueprintIT.widget.StyledTextNode({ label: '".$name."',  iconClass: 'site' }, ".$node.", true);\n");
		displaySite($id, "site".$id);
	}

	$pos=0;
	$result = $db->query('SELECT rowid,name FROM system WHERE site='.$site.';');
	while ($row = $result->fetch())
	{
		$system = "system_".$site."_".$pos;
		$name = $row['name'];
		print("\tvar ".$system." = new BlueprintIT.widget.StyledTextNode({ label: '".$name."', href: 'system.php?id=".$row['rowid']."', target: 'detail', iconClass: 'system' }, ".$node.", true);\n");
		displayComponent(0, $row['rowid'], $system);
		$pos++;
	}
}

?>
<html>
<head>
	<title>Auditor</title>

	<link rel="stylesheet" type="text/css" href="yahoo/css/folders/tree.css">

	<script src="yahoo/YAHOO.js" type="text/javascript"></script>
	<script src="yahoo/dom.js" type="text/javascript"></script>
	<script src="yahoo/event.js" type="text/javascript"></script>
	<script src="yahoo/connection.js" type="text/javascript"></script>
	<script src="yahoo/dragdrop.js" type="text/javascript"></script>
	<script src="yahoo/treeview.js" type="text/javascript"></script>
	<script src="scripts/treeview.js" type="text/javascript"></script>
</head>
<body>
<div id="tree">
</div>

<script type="text/javascript">
function loadTree(event)
{
	var tree = new YAHOO.widget.TreeView("tree");
	var root = tree.getRoot();
<?
	displaySite(0, 'root');
?>	tree.draw();
}

YAHOO.util.Event.addListener(window, "load", loadTree);
</script>
</body>
</html>
