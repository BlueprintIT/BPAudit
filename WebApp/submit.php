<?

require 'logging.php';
require 'storage.php';

LoggerManager::setLogLevel('', SWIM_LOG_WARN);

$log = LoggerManager::getLogger('core');

function error($code, $text)
{
	header($_SERVER["SERVER_PROTOCOL"]." ".$code." ".$text);
	header('Status: '.$text);
	exit;
}

function parseComponent($system, $date, $component, $element)
{
	global $db;
	
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

		$db->queryExec('INSERT INTO stringvalue (component, system, id, date, value) VALUES ('.$component.','.$system.',\''.$db->escape($node->name).'\','.$db->escape($date).',\''.$db->escape($node->value).'\');');
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
				parseComponent($system, $date, $sub, $node);
			}
			else if ($node->localName == 'value')
			{
				$name = $node->getAttribute('id');
				$type = $node->getAttribute('type');
				if ($type=='number')
				{
					$table = 'numbervalue';
					$value = $db->escape($node->getAttribute('value'));
				}
				else if ($type=='string')
				{
					$table = 'stringvalue';
					$value = '\''.$db->escape($node->getAttribute('value')).'\'';
				}
				$db->queryExec('INSERT INTO '.$table.' (component, system, id, date, value) VALUES ('.$component.','.$system.',\''.$db->escape($name).'\','.$date.','.$value.');');
			}
		}
		$node = $node->nextSibling;
	}
}

function parseAudit($document, $audit)
{
	global $db;
	
	$guid = $audit->getAttribute('id');
	$date = $audit->getAttribute('date');
	
	$dir = './audits/'.$guid;
	if (!is_dir($dir))
		mkdir($dir);
	
	$file = $dir.'/'.$date.'.xml';
	if (!is_file($file))
	{
		$document->save($file);
	
		$db = new Storage('./audits/audits.db');
		$system = $db->singleQuery('SELECT rowid FROM system WHERE uid=\''.$db->escape($guid).'\';');
		if ($system == null)
		{
			$site = 0;
			if ($audit->hasAttribute('domainname') && (strlen($audit->hasAttribute('domainname'))>0))
			{
				$sites = $db->singleQuery('SELECT rowid FROM site WHERE domain=\''.$db->escape($audit->getAttribute('domainname')).'\';');
				if (($sites != null) && (!is_array($sites)))
				{
					$site = $sites;
				}
			}
			$db->queryExec('INSERT INTO system (uid,site,lastcheckin) VALUES (\''.$db->escape($guid).'\','.$site.','.$db->escape($date).')');
			$system = $db->lastInsertRowid();
		}
		else
		{
			$db->queryExec('UPDATE system SET lastcheckin='.$db->escape($date).' WHERE id=\''.$db->escape($guid).'\' AND lastcheckin<'.$db->escape($date).';');
		}
		if ($audit->hasAttribute('hostname'))
		{
			$db->queryExec('UPDATE system SET name=\''.$db->escape($audit->getAttribute('hostname')).'\' WHERE id=\''.$db->escape($guid).'\';');
		}
		parseComponent($system, $date, 0, $audit);
	}

	?><config id="<?= $guid ?>" version="0"/><?
}

if ($_SERVER['REQUEST_METHOD'] != 'POST')
	error(405, "Method Not Allowed");

if ($_SERVER['CONTENT_TYPE'] != 'text/xml')
	error(415, "Unsupported Media Type");

$xml = file_get_contents('php://input');
$document = new DOMDocument();
if (!$document->loadXML($xml))
	error(500, "Internal Server Error");

$audit = $document->documentElement;

if (($audit->localName != 'system') || ($audit->namespaceURI != 'http://audit.blueprintit.co.uk'))
	error(415, "Unsupported Media Type");

echo "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";

parseAudit($document, $audit);

?>