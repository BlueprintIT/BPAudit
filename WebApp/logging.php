<?

define('SWIM_LOG_NONE',0);
define('SWIM_LOG_FATAL',1);
define('SWIM_LOG_ERROR',2);
define('SWIM_LOG_WARN',3);
define('SWIM_LOG_INFO',4);
define('SWIM_LOG_DEBUG',5);
define('SWIM_LOG_TRACE',5);
define('SWIM_LOG_ALL',2000);

class LogOutput
{
	protected $pattern;
	protected $tracePattern;
	private $level = SWIM_LOG_ALL;
	
	function LogOutput()
	{
		$pattern='';
		$tracePattern='';
	}
	
	function setLevel($level)
	{
		$this->level=$level;
	}
	
	function internalOutput($text)
	{
	}
	
	function convertPattern($text,$vars)
	{
		$matches=array();
		$count=preg_match_all('/\$\[([^=#$[\]]+?)\]/',$text,$matches,PREG_OFFSET_CAPTURE | PREG_PATTERN_ORDER);
		for($p=$count-1; $p>=0; $p--)
		{
			$pref=$matches[1][$p][0];
			$offset=$matches[0][$p][1];
			$length=strlen($matches[0][$p][0]);
			if (isset($vars[$pref]))
			{
				$replacement=$vars[$pref];
			}
			else
			{
				$replacement='';
			}
			$text=substr_replace($text,$replacement,$offset,$length);
		}
		return $text;
	}
	
	function convertTrace($logger,$detail)
	{
		$detail['txtlevel']='UNKNOWN';
		switch($detail['level'])
		{
			case SWIM_LOG_FATAL:
				$detail['txtlevel']='FATAL';
				break;
			case SWIM_LOG_ERROR:
				$detail['txtlevel']='ERROR';
				break;
			case SWIM_LOG_WARN:
				$detail['txtlevel']='WARN';
				break;
			case SWIM_LOG_INFO:
				$detail['txtlevel']='INFO';
				break;
			case SWIM_LOG_DEBUG:
				$detail['txtlevel']='DEBUG';
				break;
		}
		if ((isset($detail['args']))&&(count($detail['args'])>0))
		{
			$list = '';
			foreach ($detail['args'] as $arg)
			{
				if (is_string($arg))
				{
					$list.='"'.$arg.'",';
				}
				else if (is_array($arg))
				{
					$list.='Array,';
				}
				else if (is_object($arg))
				{
					$list.='Object,';
				}
				else
				{
					$list.=$arg.',';
				}
			}
			$list=substr($list,0,-1);
			$detail['arglist']='('.$list.')';
		}
		$detail['logger']=$logger->getName();
    $detail['uid']=LoggerManager::getUID();
		return $detail;
	}
	
	function output($logger,$detail)
	{
		if ($detail['level']<=$this->level)
		{
			$detail=$this->convertTrace($logger,$detail);
			$this->internalOutput($this->convertPattern($this->pattern,$detail));
		}
	}
	
	function outputTrace($logger,$detail)
	{
		if ($detail['level']<=$this->level)
		{
			$detail=$this->convertTrace($logger,$detail);
			$this->internalOutput($this->convertPattern($this->tracePattern,$detail));
		}
	}
}

class FileLogOutput extends LogOutput
{
  private $filename;
  
	function FileLogOutput($filename)
	{
		$this->LogOutput();
		$this->filename=$filename;
		$this->pattern="[$[txtlevel] $[uid]] $[logger]: $[text] ($[file]:$[line])\n";
		$this->tracePattern="$[logger]: $[function]$[arglist] ($[file]:$[line])\n";
	}
	
	function internalOutput($text)
	{
	  $file=fopen($this->filename,'a');
	  flock($file,LOCK_EX);
	  fwrite($file,$text);
	  flock($file,LOCK_UN);
	  fclose($file);
	}
}

class PageLogOutput extends LogOutput
{
	function PageLogOutput()
	{
		$this->LogOutput();
		$this->pattern='<b>[$[txtlevel]]</b> $[logger]: $[text] ($[file]:$[line])<br />';
		$this->tracePattern="$[logger]: $[function]$[arglist] ($[file]:$[line])<br />\n";
	}
	
	function internalOutput($text)
	{
		print($text."\n");
	}
}

class Logger
{
	private $name;
	private $level;
	private $parent;
	private $output;
	
	function Logger($parent,$name)
	{
		$this->name=$name;
		$this->parent=$parent;
	}
	
	function setParent($parent)
	{
		$this->parent=$parent;
	}
	
	function setLogOutput($output)
	{
		$this->output=$output;
	}
	
	function clearLogOutput()
	{
		unset($this->output);
	}
	
  function getName()
  {
    return $this->name;
  }
  
	function getLevel()
	{
		if (isset($this->level))
		{
			return $this->level;
		}
		return $this->parent->getLevel();
	}
	
	function clearLevel()
	{
		unset($this->level);
	}
	
	function setLevel($level)
	{
		$this->level=$level;
	}
	
	function getOutputter()
	{
		if (isset($this->output))
		{
			return $this->output;
		}
		else
		{
			return $this->parent->getOutputter();
		}
	}
	
	function buildStackTrace()
	{
		$trace=debug_backtrace();
		$result = array();

		$diff=3;
		$pos=0;
		$tpos=$diff;
		$count=count($trace)-$diff;
		while ($pos<$count)
		{
			if (isset($trace[$tpos]['class']))
			{
				$result[$pos]['function']=$trace[$tpos]['class'].$trace[$tpos]['type'].$trace[$tpos]['function'];
			}
			else
			{
				$result[$pos]['function']=$trace[$tpos]['function'];
			}
			$result[$pos]['args']=$trace[$tpos]['args'];
			$result[$pos]['line']=$trace[$tpos-1]['line'];
			$result[$pos]['file']=$trace[$tpos-1]['file'];
			$pos++;
			$tpos++;
		}
		if (isset($trace[$tpos-1]['line']))
		{
			$result[$pos]['line']=$trace[$tpos-1]['line'];
		}
		else
		{
			$result[$pos]['line']='-1';
		}
		if (isset($trace[$tpos-1]['file']))
		{
			$result[$pos]['file']=$trace[$tpos-1]['file'];
		}
		else
		{
			$result[$pos]['file']='Unknown';
		}
		$result[$pos]['args']=array();
		$result[$pos]['function']='';
		
		return $result;
	}
	
	function log($level,$text,$trace = null)
	{
		if ($this->getLevel()>=$level)
		{
			$out = $this->getOutputter();
			if (!isset($trace))
			{
				$trace=$this->buildStackTrace();
			}
			$trace=$trace[0];
			$trace['level']=$level;
			$trace['text']=$text;
			if (is_array($text))
			{
				foreach ($text as $name => $value)
				{
					$trace['text']='"'.$name.'" => "'.$value.'"';
				}
			}
			$out->output($this,$trace);
		}
	}
	
	function logtrace($level,$text,$trace = null)
	{
		if ($this->getLevel()>=$level)
		{
			$out = $this->getOutputter();

			if (!isset($trace))
			{
				$trace=$this->buildStackTrace();
			}
			$base=$trace[0];
			$base['level']=$level;
			$base['text']=$text;
			if (is_array($text))
			{
				foreach ($text as $name => $value)
				{
					$base['text']='"'.$name.'" => "'.$value.'"';
					$out->output($this,$base);
				}
			}
			else
			{
				$out->output($this,$base);
			}

			foreach ($trace as $line)
			{
				$line['level']=$level;
				$out->outputTrace($this,$line);
			}
		}
	}
	
	function fatal($text)
	{
		$this->log(SWIM_LOG_FATAL,$text);
	}
	
	function fataltrace($text)
	{
		$this->logtrace(SWIM_LOG_FATAL,$text);
	}
	
	function error($text)
	{
		$this->log(SWIM_LOG_ERROR,$text);
	}
	
	function errortrace($text)
	{
		$this->logtrace(SWIM_LOG_ERROR,$text);
	}
	
	function warn($text)
	{
		$this->log(SWIM_LOG_WARN,$text);
	}
	
	function warntrace($text)
	{
		$this->logtrace(SWIM_LOG_WARN,$text);
	}
	
	function info($text)
	{
		$this->log(SWIM_LOG_INFO,$text);
	}
	
	function infotrace($text)
	{
		$this->logtrace(SWIM_LOG_INFO,$text);
	}
	
  function debug($text)
  {
    $this->log(SWIM_LOG_DEBUG,$text);
  }
  
  function debugtrace($text)
  {
    $this->logtrace(SWIM_LOG_DEBUG,$text);
  }
  
  function trace($text)
  {
    $this->log(SWIM_LOG_TRACE,$text);
  }
  
  function tracetrace($text)
  {
    $this->logtrace(SWIM_LOG_TRACE,$text);
  }
  
	function isFatalEnabled()
	{
		return $this->getLevel()>=SWIM_LOG_FATAL;
	}
	
	function isErrorEnabled()
	{
		return $this->getLevel()>=SWIM_LOG_ERROR;
	}
	
	function isWarnEnabled()
	{
		return $this->getLevel()>=SWIM_LOG_WARN;
	}
	
	function isInfoEnabled()
	{
		return $this->getLevel()>=SWIM_LOG_INFO;
	}
  
  function isDebugEnabled()
  {
    return $this->getLevel()>=SWIM_LOG_DEBUG;
  }
  
  function isTraceEnabled()
  {
    return $this->getLevel()>=SWIM_LOG_TRACE;
  }
}

class LoggerManager
{
	private static $loggers = array();
  private static $root;
  private static $uids = array();
	
	static function init()
	{
    self::$root = new Logger(null,'');
    self::$root->setLevel(SWIM_LOG_WARN);
		self::setLogOutput('',new FileLogOutput('audits.log'));
    self::pushState();
	}
  
  static function getUID()
  {
    return self::$uids[count(self::$uids)-1];
  }
  
  static function pushState()
  {
    $uid=rand(1000,9999);
    array_push(self::$uids, $uid);
  }
  
  static function popState()
  {
    array_pop(self::$uids);
  }
	
	static function getOutputter()
	{
		return self::$output;
	}
	
	static function getLevel()
	{
		return self::$root->getLevel();
	}
	
	static function createLogger($name)
	{
		if (!isset(self::$loggers[$name]))
		{
			$logger = new Logger(self::$root,$name);

			$bestcount=0;
			$bestparent=self::$root;
			foreach (array_keys(self::$loggers) as $key)
			{
				if (strlen($key)<strlen($name))
				{
					if ((strlen($key)>$bestcount)&&(substr($name,0,strlen($key))==$key))
					{
						$bestcount=strlen($key);
						$bestparent=self::$loggers[$key];
					}
				}
				else if (strlen($key)>strlen($name))
				{
					if (substr($key,0,strlen($name))==$name)
					{
						$tlogger=self::$loggers[$key];
						$tlogger->setParent($logger);
					}
				}
			}
			$logger->setParent($bestparent);
			self::$loggers[$name]=$logger;
      $logger->trace('Logger created');
		}
	}
	
	static function setLogLevel($prefix,$level)
	{
		if (strlen($prefix)>0)
		{
			$logger=self::getLogger($prefix);
			$logger->setLevel($level);
		}
		else
		{
			self::$root->setLevel($level);
		}
	}
	
	static function clearLogLevel($prefix)
	{
		if (strlen($prefix)>0)
		{
			$logger=self::getLogger($prefix);
			$logger->clearLevel();
		}
		else
		{
		}
	}
	
	static function setLogOutput($prefix,$output)
	{
		if (strlen($prefix)>0)
		{
			$logger=self::getLogger($prefix);
			$logger->setLogOutput($output);
		}
		else
		{
			self::$root->setLogOutput($output);
		}
	}
	
	static function getLogger($name)
	{
		if (!isset(self::$loggers[$name]))
		{
			self::createLogger($name);
		}
		return self::$loggers[$name];
	}
	
	static function shutdown()
	{
	}
}

LoggerManager::init();

function caught_error($type,$text,$file,$line)
{
	// Filter out lines marked with @ for expected errors.
	if (ini_get('error_reporting')==0)
		return;

	$log = LoggerManager::getLogger('php');
	
	$trace = array('file' => $file, 'line' => $line, 'function' => '');
	$trace = array($trace);
	
	if (($type==E_ERROR)||($type==E_PARSE)||($type==E_CORE_ERROR)||($type==E_COMPILE_ERROR))
	{
		$log->log(SWIM_LOG_FATAL,$text,$trace);
	}
	else if (($type==E_WARNING)||($type==E_CORE_WARNING)||($type==E_COMPILE_WARNING)||($type==E_USER_ERROR))
	{
		$log->log(SWIM_LOG_ERROR,$text,$trace);
	}
	else if (($type==E_NOTICE)||($type==E_USER_WARNING))
	{
		$log->log(SWIM_LOG_WARN,$text,$trace);
	}
	else if ($type==E_USER_NOTICE)
	{
		$log->log(SWIM_LOG_INFO,$text,$trace);
	}
	else if ($type==2048)
	{
		$log->log(SWIM_LOG_DEBUG,$text,$trace);
	}
}

set_error_handler('caught_error');

?>