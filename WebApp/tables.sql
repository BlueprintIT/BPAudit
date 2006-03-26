DROP TABLE IF EXISTS site;
CREATE TABLE IF NOT EXISTS site (
  rowid INTEGER NOT NULL auto_increment,
  parent INTEGER,
  name VARCHAR(50),
  domain VARCHAR(30),
  PRIMARY KEY (rowid)
);

DROP TABLE IF EXISTS system;
CREATE TABLE IF NOT EXISTS system (
  rowid INTEGER NOT NULL auto_increment,
  uid char(36) NOT NULL,
  name varchar(50),
  description TEXT,
  site INTEGER,
  lastcheckin INTEGER,
  PRIMARY KEY (rowid)
);

DROP TABLE IF EXISTS component;
CREATE TABLE IF NOT EXISTS component (
  rowid INTEGER NOT NULL auto_increment,
  parent INTEGER,
  id VARCHAR(50),
  PRIMARY KEY (rowid)
);

DROP TABLE IF EXISTS value;
CREATE TABLE IF NOT EXISTS value (
  system INTEGER NOT NULL,
  component INTEGER NOT NULL,
  id VARCHAR(50) NOT NULL,
  `date` INTEGER,
  stringvalue TEXT,
  numbervalue BIGINT,
  list INTEGER,
  PRIMARY KEY (system, component, id, date)
);

DROP TABLE IF EXISTS listvalue;
CREATE TABLE IF NOT EXISTS listvalue (
  list INTEGER NOT NULL auto_increment,
  `value` TEXT,
  PRIMARY KEY (list)
);
