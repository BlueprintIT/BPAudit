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
  id VARCHAR(30),
  PRIMARY KEY (rowid)
);

DROP TABLE IF EXISTS list;
CREATE TABLE IF NOT EXISTS list (
  rowid INTEGER NOT NULL auto_increment,
  system INTEGER NOT NULL,
  component INTEGER NOT NULL,
  `date` INTEGER,
  id VARCHAR(30),
  PRIMARY KEY (rowid)
);

DROP TABLE IF EXISTS listvalue;
CREATE TABLE IF NOT EXISTS listvalue (
  list INTEGER NOT NULL,
  `value` TEXT
);

DROP TABLE IF EXISTS numbervalue;
CREATE TABLE IF NOT EXISTS numbervalue (
  system INTEGER NOT NULL,
  component INTEGER NOT NULL,
  id VARCHAR(30) NOT NULL,
  `date` INTEGER,
  `value` INTEGER,
  PRIMARY KEY (system, component, id)
);

DROP TABLE IF EXISTS stringvalue;
CREATE TABLE IF NOT EXISTS stringvalue (
  system INTEGER NOT NULL,
  component INTEGER NOT NULL,
  id VARCHAR(30) NOT NULL,
  `date` INTEGER,
  `value` TEXT,
  PRIMARY KEY (system, component, id)
);
