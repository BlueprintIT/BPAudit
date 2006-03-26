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

function addList($system, $date, $component, $name, $element)
{
	global $db;
	
	$first = true;
	$node = $element->firstChild;
	while ($node !== null)
	{
		if ($node->nodeType == XML_ELEMENT_NODE)
		{
			if ($node->localName == 'item')
			{
				$value = $db->escape($node->getAttribute('value'));
				
				if ($first)
				{
					$db->queryExec('INSERT INTO listvalue (value) VALUES (\''.$value.'\');');
					$id = $db->lastInsertRowid();
					$db->queryExec('INSERT INTO value (component, system, id, date, list) VALUES ('.$component.','.$system.',\''.$db->escape($name).'\','.$date.','.$id.');');
					$first=false;
				}
				else
				{
					$db->queryExec('INSERT INTO listvalue (id,value) VALUES ('.$id.',\''.$value.'\');');
				}
			}
		}
		$node = $node->nextSibling;
	}
	if ($first)
	{
		$db->queryExec('INSERT INTO value (component, system, id, date, list) VALUES ('.$component.','.$system.',\''.$db->escape($name).'\','.$date.',0);');
	}
}

function addValue($system, $date, $component, $name, $value, $type)
{
	global $db;
	
	if ($type=='number')
	{
		$column = 'numbervalue';
		$value = $db->escape($value);
	}
	else if ($type=='string')
	{
		$column = 'stringvalue';
		$value = '\''.$db->escape($value).'\'';
	}
	$db->queryExec('INSERT INTO value (component, system, id, date, '.$column.', list) VALUES ('.$component.','.$system.',\''.$db->escape($name).'\','.$date.','.$value.',0);');
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

		addValue($system, $date, $component, $node->name, $node->value, 'string');
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
				addValue($system, $date, $component, $node->getAttribute('id'), $node->getAttribute('value'), $node->getAttribute('type'));
			}
			else if ($node->localName == 'list')
			{
				addList($system, $date, $component, $node->getAttribute('id'), $node);
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
			$db->queryExec('UPDATE system SET name=\''.$db->escape($audit->getAttribute('hostname')).'\' WHERE uid=\''.$db->escape($guid).'\';');
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