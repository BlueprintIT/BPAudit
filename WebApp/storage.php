<?

class StorageResult
{
	private $result;
	private $offset = 0;
	
	public function StorageResult($result)
	{
		$this->result = $result;
	}
	
	public function fetch()
	{
		$this->offset++;
		return $this->result->fetch_row();
	}
	
	public function fetchObject()
	{
		$this->offset++;
		return $this->result->fetch_object();
	}
	
	public function fetchSingle()
	{
		$row = $this->fetch();
		return $row[0];
	}
	
	public function fetchAll()
	{
		$total = array();
		while ($this->valid())
		{
			array_push($total, $this->fetch());
		}
		return $total;
	}
	
	public function column($index)
	{
		$row = $this->current();
		if ($row)
			return $row[$index];
	}
	
	public function numFields()
	{
		return $this->result->field_count;
	}
	
	public function fieldName($index)
	{
		return $this->result->fetch_field_direct($index);
	}
	
	public function current()
	{
		$row = $this->fetch();
		$this->prev();
		return $row;
	}
	
	public function key()
	{
		return $this->offset;
	}
	
	public function next()
	{
		return $this->seek($this->offset+1);
	}
	
	public function valid()
	{
		return $this->offset<$this->numRows();
	}
	
	public function rewind()
	{
		$this->seek(0);
	}
	
	public function prev()
	{
		if ($this->hasPrev())
			return $this->seek($this->offset-1);
		else
			return false;
	}
	
	public function hasPrev()
	{
		return $this->offset>0;
	}
	
	public function numRows()
	{
		return $this->result->num_rows;
	}
	
	public function seek($pos)
	{
		$result = $this->result->data_seek($pos);
		if ($result)
			$this->offset = $pos;
		return $result;
	}
}

class Storage
{
  private $db;
  private $log;
  
  public function Storage($filename)
  {
    $this->log = LoggerManager::getLogger('storage');
    $this->db = new mysqli("localhost", "blueprin_audits", "aud652", "blueprin_audits");
    $this->log->debug('Loaded database from '.$filename);
  }
  
  public function escape($text)
  {
    return $this->db->escape_string($text);
  }
  
  public function query($query)
  {
    $this->log->debug('query: '.$query);
    $result = $this->db->query($query);
    if ($result && $result !== TRUE)
    	return new StorageResult($result);
    return $result;
  }
  
  public function queryExec($query)
  {
    $this->log->debug('queryExec: '.$query);
    return $this->db->query($query);
  }
  
  public function arrayQuery($query)
  {
    $this->log->debug('arrayQuery: '.$query);
    $result = $this->query($query);
    if ($result)
    	return $result->fetchAll();
    return $result;
  }
  
  public function singleQuery($query)
  {
    $this->log->debug('singleQuery: '.$query);
    $result = $this->query($query);
    if ($result)
    {
    	if ($result->numRows()==0)
    	{
    		return null;
    	}
    	if ($result->numRows()>1)
    	{
    		$total = array();
    		while ($row = $result->fetch())
    		{
    			array_push($total, $row[0]);
    		}
    		return $total;
    	}
    	return $result->fetchSingle();
    }
    return $result;
  }
  
  public function lastInsertRowid()
  {
    return $this->db->insert_id;
  }
  
  public function changes()
  {
    return $this->db->affected_rows;
  }
  
  public function lastError()
  {
    return $this->db->errno;
  }
}

?>