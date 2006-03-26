<?

require 'logging.php';
require 'storage.php';
require 'audit.php';

LoggerManager::setLogLevel('', SWIM_LOG_WARN);

$log = LoggerManager::getLogger('core');

$db = new Storage("");

$component = $_GET['id'];
$system = $_GET['system'];

$name = $db->singleQuery('SELECT id FROM component WHERE rowid='.$component.';');

?>
<html>
<head>
	<title>Component Details</title>
</head>
<body>
<h1><?= $name ?></h1>
<?
$values = $db->query('SELECT value.id,value.stringvalue,value.numbervalue,value.list FROM value, '
                    .'(SELECT id,max(date) AS date FROM value WHERE system='.$system.' AND component='.$component.' GROUP BY id) AS subtable '
                    .'WHERE subtable.id=value.id and subtable.date=value.date AND system='.$system.' AND component='.$component.';');

if ($values->valid())
{
?>	<dl>
<?
	while ($row = $values->fetch())
	{
?>
		<dt><?= $row['id'] ?></dt>
		<dd><?
		if ($row['stringvalue'] !== null)
		{
			print($row['stringvalue']);
		}
		else if ($row['numbervalue'] !== null)
		{
			print($row['numbervalue']);
		}
		else
		{
			print("List");
		}
 ?></dd>
<?
	}
?>	</dl>
<?
}

$variables = $db->query('SELECT DISTINCT value.id FROM value JOIN component ON value.component=component.rowid WHERE component.parent='.$component.' and value.system='.$system.';');
$valnames = array();
?>
<table>
	<thead>
		<tr>
			<th></th>
<?
while ($name = $variables->fetchSingle())
{
	array_push($valnames, $name);
	print("\t\t\t<th>".$name."</th>\n");
}
?>
		</tr>
	</thead>
	<tbody>
<?
$components = $db->query('SELECT rowid,id FROM component WHERE parent='.$component.';');

while ($row = $components->fetch())
{
?>
		<tr>
			<td><a href="component.php?id=<?= $row['rowid'] ?>&amp;system=<?= $system ?>"><?= $row['id'] ?></a></td>
<?
	foreach ($valnames as $name)
	{
		$value = fetchCurrentValue($row['rowid'], $system, $name);
		print("\t\t\t<td>");
		if (is_array($value['value']))
		{
			foreach ($value['value'] as $val)
			{
				print($val."<br>");
			}
		}
		else
		{
			print($value['value']);
		}
		print("</td>\n");
	}
?>
		</tr>
<?
}

?>
	</tbody>
</table>
</body>
</html>
