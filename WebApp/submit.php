<?

require 'logging.php';
require 'storage.php';

LoggerManager::setLogLevel('', SWIM_LOG_WARN);

$log = LoggerManager::getLogger('core');
$reftime = time();
$starttime = $reftime;
$log->warn('Startup');

function error($code, $text)
{
	header($_SERVER["SERVER_PROTOCOL"]." ".$code." ".$text);
	header('Status: '.$text);
	exit;
}

function parseComponent($db, $date, $component, $element)
{
	$attrs = $element->attributes;
	$pos = -1;
	while (true)
	{
		$pos++;
		$node = $attrs->item($pos);
		if ($node == null)
			break;
			
		if ($node->name == 'id')
			continue;

		$db->queryExec('INSERT INTO value (component, id, date, value) VALUES ('.$component.',\''.$db->escape($node->name).'\','.$db->escape($date).',\''.$db->escape($node->value).'\');');
	}
	
	$node = $element->firstChild;
	while ($node !== null)
	{
		if ($node->nodeType == XML_ELEMENT_NODE)
		{
			if ($node->localName == 'component')
			{
				$sub = $db->singleQuery('SELECT rowid FROM component WHERE parent='.$component.' AND id=\''.$db->escape($node->getAttribute('id')).'\';');
				if ($sub == false)
				{
					$db->queryExec('INSERT INTO component (parent,id) VALUES ('.$component.',\''.$db->escape($node->getAttribute('id')).'\');');
					$sub = $db->lastInsertRowid();
				}
				parseComponent($db, $date, $sub, $node);
			}
		}
		$node = $node->nextSibling;
	}
}

if ($_SERVER['REQUEST_METHOD'] != 'POST')
	error(405, "Method Not Allowed");

if ($_SERVER['CONTENT_TYPE'] != 'text/xml')
	error(415, "Unsupported Media Type");

$log->warn('init time: '.(time()-$reftime));
$reftime = time();

$xml = file_get_contents('php://input');
$document = new DOMDocument();
if (!$document->loadXML($xml))
	error(500, "Internal Server Error");

$log->warn('load time: '.(time()-$reftime));
$reftime = time();

$audit = $document->documentElement;

if (($audit->localName != 'audit') || ($audit->namespaceURI != 'http://audit.blueprintit.co.uk'))
	error(415, "Unsupported Media Type");

$guid = $audit->getAttribute('id');
$date = $audit->getAttribute('date');

$dir = './audits/'.$guid;
if (!is_dir($dir))
	mkdir($dir);

$file = $dir.'/'.$date.'.xml';
if (!is_file($file))
{
	$document->save($file);

	$log->warn('save time: '.(time()-$reftime));
	$reftime = time();

	$db = new Storage('./audits/audits.db');
	$result = $db->singleQuery('SELECT rowid FROM system WHERE id=\''.$db->escape($guid).'\';');
	if ($result == null)
	{
		$site = 0;
		if ($audit->hasAttribute('domainname'))
		{
			$sites = $db->singleQuery('SELECT rowid FROM site WHERE domain=\''.$db->escape($audit->getAttribute('domainname')).'\';');
			if (($sites != null) && (!is_array($sites)))
			{
				$site = $sites;
			}
		}
		$db->queryExec('INSERT INTO component (id) VALUES (\'system\');');
		$component = $db->lastInsertRowid();
		$db->queryExec('INSERT INTO system (id,site,component,lastcheckin) VALUES (\''.$db->escape($guid).'\','.$site.','.$component.','.$db->escape($date).')');
	}
	else
	{
		$db->queryExec('UPDATE system SET lastcheckin='.$db->escape($date).' WHERE id=\''.$db->escape($guid).'\' AND lastcheckin<'.$db->escape($date).';');
		$component = $db->singleQuery('SELECT component FROM system WHERE id=\''.$db->escape($guid).'\';');
	}
	if ($audit->hasAttribute('hostname'))
	{
		$db->queryExec('UPDATE system SET name=\''.$db->escape($audit->getAttribute('hostname')).'\' WHERE id=\''.$db->escape($guid).'\';');
	}
	$log->warn('preparse time: '.(time()-$reftime));
	$reftime = time();
	parseComponent($db, $date, $component, $audit);
	$log->warn('parse time: '.(time()-$reftime));
	$reftime = time();
}

echo "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
?>

<config id="<?= $guid ?>" version="0"/>
<?
	$log->warn('final time: '.(time()-$reftime));
	$log->warn('complete time: '.(time()-$starttime));
?>