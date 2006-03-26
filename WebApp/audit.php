<?

function fetchCurrentValue($component, $system, $name)
{
	global $db;
	
	$result = $db->query('SELECT stringvalue, numbervalue, list, date FROM value WHERE '
	                    .'date=(SELECT MAX(date) FROM value WHERE component='.$component.' AND system='.$system.' and id=\''.$db->escape($name).'\') '
	                    .'AND component='.$component.' AND system='.$system.' AND id=\''.$db->escape($name).'\';');
	
	if ($result->valid())
	{
		$row = $result->fetch();
		
		if ($row['stringvalue'] !== null)
		{
			$row['value']=$row['stringvalue'];
		}
		else if ($row['numbervalue'] !== null)
		{
			$row['value']=$row['numbervalue'];
		}
		else
		{
			$items = array();
			$list = $db->query('SELECT value FROM listvalue WHERE list='.$row['list'].';');
			while ($item = $list->fetch())
			{
				array_push($items, $item['value']);
			}
			$row['value'] = $items;
		}
		return $row;
	}
	return null;
}

?>