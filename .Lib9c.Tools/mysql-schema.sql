CREATE TABLE IF NOT EXISTS `tx_references` (
    CONSTRAINT `uid` UNIQUE (`tx_id`, `block_hash`),

    `tx_id`         VARCHAR,
    `block_hash`    VARCHAR,
    `tx_nonce`      BIGINT
);

CREATE TABLE IF NOT EXISTS `signer_references` (
    CONSTRAINT `uid` UNIQUE (`signer`, `tx_id`),

    `signer`    VARCHAR,
    `tx_id`     VARCHAR,
    `tx_nonce`  BIGINT
);

CREATE TABLE IF NOT EXISTS `updated_address_references` (
    CONSTRAINT `uid` UNIQUE (`updated_address`, `tx_id`),

    `updated_address`   VARCHAR,
    `tx_id`             VARCHAR,
    `tx_nonce`          BIGINT
);

CREATE TABLE IF NOT EXISTS `block` (
  `index`                 BIGINT,
  `hash`                  VARCHAR,
  `pre_evaluation_hash`   VARCHAR,
  `state_root_hash`       VARCHAR,
  `difficulty`            BIGINT,
  `total_difficulty`      BIGINT,
  `nonce`                 VARCHAR,
  `miner`                 VARCHAR,
  `previous_hash`         VARCHAR,
  `timestamp`             VARCHAR,
  `tx_hash`               VARCHAR,
  `protocol_version`      INT,
    PRIMARY KEY (`hash`),
    UNIQUE INDEX `hash_UNIQUE` (`hash` ASC)
);

CREATE TABLE IF NOT EXISTS `transaction` (
  `tx_id`               VARCHAR,
  `nonce`               BIGINT,
  `signer`              VARCHAR,
  `signature`           VARCHAR,
  `timestamp`           VARCHAR,
  `public_key`          VARCHAR,
  `genesis_hash`        VARCHAR,
  `bytes_length`        INT,
  PRIMARY KEY (`tx_id`),
  UNIQUE INDEX `tx_id_UNIQUE` (`tx_id` ASC)
);
