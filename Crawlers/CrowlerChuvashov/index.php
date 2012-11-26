<?php

$username = "AGENTxXx";
$password = "*****";
$mongo = new Mongo("mongodb://{$username}:{$password}@ds043027.mongolab.com:43027/mydb");


set_time_limit(3000);
$count_page = 5; //Количество страниц, которые нужно пропарсить;
$current_page = 1;

$next_page ="";

require_once 'simple_html_dom.php'; 
require_once('stemmereng.php');

$stemka = new PorterStemmer();

$pars = array();

for($i=0; $i<12*$count_page; $i++)
{
	$pars[$i] = array();
}

$i = 0; //Счетчик

$data = file_get_html('http://www.amazon.com/s/ref=lp_465600_nr_n_0?rh=n%3A283155%2Cn%3A%212349030011%2Cn%3A465600%2Cn%3A468220&bbn=465600&ie=UTF8&qid=1350916001&rnid=465600');
for ($k = 0; $k < $count_page; $k++)
{
foreach($data->find('script,link,comment') as $tmp)$tmp->outertext = '';

for ($i = 12*($current_page-1); $i < 12*$current_page; $i++)
{
if($data->innertext!='' && count($data->find('#result_'. $i)))
{
	foreach($data->find('#result_'. $i) as $block_div);
	
	
	foreach($block_div->find('a.title') as $book_name);
	$pars[$i]['book_name'] =  $book_name->plaintext;
	
	foreach($block_div->find('div.image a') as $book_link);
	$pars[$i]['book_link'] = $book_link->href;
	
	
	foreach($book_link->find('img.productImage') as $book_image);
	$pars[$i]['book_image'] = $book_image->src;
	
	
	foreach($block_div->find('div.reviewsCount a.longReview') as $book_top);
	$pars[$i]['book_top'] = $book_top->alt;
	
	foreach($block_div->find('td.toeListPrice') as $book_price)
	{
		$pars[$i]['book_price_parse'] = $book_price->parent()->plaintext;
		$pos = strpos($pars[$i]['book_price_parse'],'$'). "<br/>";
		$str_baks = substr($pars[$i]['book_price_parse'], $pos, strlen($pars[$i]['book_price_parse']) - $pos);
		$arr_price = explode('$',$str_baks);
		$pars[$i]['book_price_strike'] = $arr_price[1];
		$pars[$i]['book_price_price'] = $arr_price[2];
		$pars[$i]['book_price_new'] = $arr_price[3];
		$pars[$i]['book_price_used'] = $arr_price[4];
		break;
	}
	unset($book_top); 
	unset($book_image); 
	unset($book_link); 
	unset($book_name); 
	unset($block_div); 
	
	$data2 = file_get_html($pars[$i]['book_link']); 
	foreach($data2->find('script,link,comment') as $tmp)$tmp->outertext = '';
	foreach($data2->find('div.content #postBodyPS') as $book_descript);
    $pars[$i]['book_desc'] = $book_descript->plaintext;
    
    
	foreach($data2->find('div.buying span a') as $book_author)
    {
        $pars[$i]['book_author'] = $book_author->plaintext;
        break;
    }
	
    $pars[$i]['book_author'] = $book_author->plaintext;
    
    $words = explode(" ", $pars[$i]['book_desc']);

    $pars[$i]['book_desc_stemmer'] = "";
    foreach($words as $word)
    {
        $pars[$i]['book_desc_stemmer'] .= $stemka->Stem($word) ." ";
    }
	
	}
}
	foreach($data->find('span.pagnLink a') as $block_page)
	{
		if ($current_page < (int)$block_page->plaintext)
		{
			$current_page = (int)$block_page->plaintext;
			$next_page = "http://www.amazon.com".$block_page->href;
			
			$data = null;
			$data = file_get_html($next_page);
			break;
		}
	}
	if ($current_page < 0 || $current_page > $count_page)
	{
		break;
	}
	sleep(10);
}
	echo "<pre>";
	print_r($pars);
	echo "</pre>";



for($i=0; $i<12*$count_page; $i++)
{
	if ($pars[$i]['book_name'] != "")
	$mongo->mydb->books->update(
		array('book_name' => $pars[$i]['book_name']),
		array(
			'book_name' => $pars[$i]['book_name'],
			'book_link' => $pars[$i]['book_link'],
			'book_image' => $pars[$i]['book_image'],
			'book_top' => $pars[$i]['book_top'],
			'book_price_strike' => $pars[$i]['book_price_strike'],
			'book_price_price' => $pars[$i]['book_price_price'],
			'book_price_used' => $pars[$i]['book_price_used'],
			'book_desc' => $pars[$i]['book_desc'],
			'book_desc_stemmer' => $pars[$i]['book_desc_stemmer'],
			'book_author' => $pars[$i]['book_author']
		),
		array('upsert' => true)
	);
}


?>